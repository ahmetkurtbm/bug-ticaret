using Microsoft.EntityFrameworkCore;
using Tezgah.Application.Campaigns;
using Tezgah.Application.Common.Exceptions;
using Tezgah.Application.Common.Interfaces;
using Tezgah.Application.Pricing;
using Tezgah.Domain.Campaigns;
using Tezgah.Domain.Carts;
using Tezgah.Domain.Catalog;
using Tezgah.Domain.Common;

namespace Tezgah.Application.Carts;

public class CartService(ITezgahDbContext db, CouponValidator coupons, TimeProvider clock) : ICartService
{
    private const int MaxQuantityPerLine = 20;

    public async Task<CartDto> GetAsync(CartOwner owner, CancellationToken ct)
    {
        var cart = await FindAsync(owner, ct);
        return cart is null ? Empty() : await BuildAsync(cart, ct);
    }

    public async Task<CartDto> AddItemAsync(CartOwner owner, AddCartItemRequest request, CancellationToken ct)
    {
        var variant = await db.ProductVariants
            .Include(v => v.Product)
            .SingleOrDefaultAsync(v => v.Id == request.VariantId, ct)
            ?? throw new NotFoundException("Ürün bulunamadı.");
        if (!variant.IsActive || variant.Product.Status != ProductStatus.Active)
            throw new DomainException("variant_unavailable", "Bu ürün şu anda satışta değil.");

        var cart = await FindAsync(owner, ct) ?? Create(owner);
        var line = cart.Items.SingleOrDefault(i => i.VariantId == variant.Id);
        var newQuantity = (line?.Quantity ?? 0) + request.Quantity;

        await EnsureStockAsync(variant.Id, newQuantity, ct);

        if (line is null)
            cart.Items.Add(new CartItem { VariantId = variant.Id, Quantity = newQuantity, UnitPriceSnapshot = variant.Price });
        else
            line.Quantity = newQuantity;

        cart.UpdatedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
        return await BuildAsync(cart, ct);
    }

    public async Task<CartDto> UpdateItemAsync(CartOwner owner, long itemId, int quantity, CancellationToken ct)
    {
        var cart = await FindAsync(owner, ct) ?? throw new NotFoundException("Sepet bulunamadı.");
        var line = cart.Items.SingleOrDefault(i => i.Id == itemId) ?? throw new NotFoundException("Sepet satırı bulunamadı.");

        await EnsureStockAsync(line.VariantId, quantity, ct);
        line.Quantity = quantity;
        cart.UpdatedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
        return await BuildAsync(cart, ct);
    }

    public async Task<CartDto> RemoveItemAsync(CartOwner owner, long itemId, CancellationToken ct)
    {
        var cart = await FindAsync(owner, ct) ?? throw new NotFoundException("Sepet bulunamadı.");
        var line = cart.Items.SingleOrDefault(i => i.Id == itemId);
        if (line is not null)
        {
            cart.Items.Remove(line);
            db.CartItems.Remove(line);
            cart.UpdatedAt = clock.GetUtcNow();
            await db.SaveChangesAsync(ct);
        }
        return await BuildAsync(cart, ct);
    }

    public async Task<CartDto> ApplyCouponAsync(CartOwner owner, string couponCode, CancellationToken ct)
    {
        var cart = await FindAsync(owner, ct) ?? throw new DomainException("cart_empty", "Sepetiniz boş.");
        var coupon = await coupons.ValidateAsync(couponCode, owner.UserId, ct);

        cart.CouponCode = coupon.Code;
        cart.UpdatedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
        return await BuildAsync(cart, ct);
    }

    public async Task<CartDto> RemoveCouponAsync(CartOwner owner, CancellationToken ct)
    {
        var cart = await FindAsync(owner, ct) ?? throw new NotFoundException("Sepet bulunamadı.");
        cart.CouponCode = null;
        await db.SaveChangesAsync(ct);
        return await BuildAsync(cart, ct);
    }

    public async Task MergeAsync(Guid anonymousId, long userId, CancellationToken ct)
    {
        var guest = await db.Carts.Include(c => c.Items).SingleOrDefaultAsync(c => c.AnonymousId == anonymousId, ct);
        if (guest is null || guest.Items.Count == 0) return;

        var userCart = await db.Carts.Include(c => c.Items).SingleOrDefaultAsync(c => c.UserId == userId, ct);
        if (userCart is null)
        {
            guest.AnonymousId = null;
            guest.UserId = userId;
        }
        else
        {
            foreach (var item in guest.Items)
            {
                var existing = userCart.Items.SingleOrDefault(i => i.VariantId == item.VariantId);
                if (existing is null)
                    userCart.Items.Add(new CartItem { VariantId = item.VariantId, Quantity = item.Quantity, UnitPriceSnapshot = item.UnitPriceSnapshot });
                else
                    existing.Quantity = Math.Min(MaxQuantityPerLine, existing.Quantity + item.Quantity);
            }
            userCart.CouponCode ??= guest.CouponCode;
            db.Carts.Remove(guest);
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task<Cart?> FindAsync(CartOwner owner, CancellationToken ct)
    {
        if (owner.UserId is { } userId)
            return await db.Carts.Include(c => c.Items).SingleOrDefaultAsync(c => c.UserId == userId, ct);
        if (owner.AnonymousId is { } anonymousId)
            return await db.Carts.Include(c => c.Items).SingleOrDefaultAsync(c => c.AnonymousId == anonymousId, ct);
        return null;
    }

    private Cart Create(CartOwner owner)
    {
        if (owner.UserId is null && owner.AnonymousId is null)
            throw new DomainException("cart_owner_missing", "Sepet kimliği gönderilmedi.");
        var cart = new Cart { UserId = owner.UserId, AnonymousId = owner.UserId is null ? owner.AnonymousId : null };
        db.Carts.Add(cart);
        return cart;
    }

    private async Task EnsureStockAsync(long variantId, int quantity, CancellationToken ct)
    {
        if (quantity > MaxQuantityPerLine)
            throw new DomainException("quantity_limit", $"Bir üründen en fazla {MaxQuantityPerLine} adet alabilirsiniz.");

        var available = await db.StockItems
            .Where(s => s.VariantId == variantId)
            .SumAsync(s => s.Quantity - s.Reserved, ct);
        if (available < quantity)
            throw new DomainException("insufficient_stock", available == 0
                ? "Bu ürün tükendi."
                : $"Bu üründen en fazla {available} adet ekleyebilirsiniz.");
    }

    private async Task<CartDto> BuildAsync(Cart cart, CancellationToken ct)
    {
        if (cart.Items.Count == 0) return Empty(cart.CouponCode);

        var variantIds = cart.Items.Select(i => i.VariantId).ToList();
        var details = await db.ProductVariants.AsNoTracking()
            .Where(v => variantIds.Contains(v.Id))
            .Select(v => new
            {
                v.Id,
                v.ProductId,
                ProductName = v.Product.Name,
                ProductSlug = v.Product.Slug,
                v.Color,
                v.Size,
                v.Price,
                v.VatRate,
                v.Product.SellerId,
                SellerName = v.Product.Seller.StoreName,
                ImageUrl = v.Product.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                Available = db.StockItems.Where(s => s.VariantId == v.Id).Sum(s => s.Quantity - s.Reserved),
            })
            .ToDictionaryAsync(v => v.Id, ct);

        var items = cart.Items.Where(i => details.ContainsKey(i.VariantId)).OrderBy(i => i.Id).ToList();
        var pricingLines = items
            .Select(i => new PricingLine(i.VariantId, details[i.VariantId].SellerId, details[i.VariantId].Price, i.Quantity, details[i.VariantId].VatRate))
            .ToList();

        CouponRule? rule = null;
        string? couponError = null;
        if (cart.CouponCode is not null)
        {
            var coupon = await db.Coupons.AsNoTracking().SingleOrDefaultAsync(c => c.Code == cart.CouponCode, ct);
            var now = clock.GetUtcNow();
            if (coupon is null || !coupon.IsActive || now < coupon.StartsAt || now >= coupon.EndsAt)
                couponError = "Kupon artık geçerli değil.";
            else
                rule = new CouponRule(coupon.Code, coupon.Type, coupon.Value, coupon.MaxDiscountAmount, coupon.MinOrderAmount, coupon.SellerId);
        }

        var pricing = CartPricingCalculator.Calculate(pricingLines, rule);
        var lines = items.Select(i =>
        {
            var d = details[i.VariantId];
            var discount = pricing.LineDiscounts[i.VariantId];
            return new CartLineDto(
                i.Id, i.VariantId, d.ProductId, d.ProductName, d.ProductSlug, VariantLabel(d.Color, d.Size), d.ImageUrl,
                d.SellerName, d.Price, d.Price != i.UnitPriceSnapshot, i.Quantity, d.Available, discount, d.Price * i.Quantity - discount);
        }).ToList();

        var afterDiscount = pricing.Subtotal - pricing.DiscountTotal;
        return new CartDto(
            lines,
            lines.Sum(l => l.Quantity),
            pricing.Subtotal,
            pricing.DiscountTotal,
            pricing.ShippingTotal,
            pricing.VatTotal,
            pricing.GrandTotal,
            cart.CouponCode,
            couponError ?? pricing.CouponError,
            // Kargo zaten bedavaysa (eşik aşıldı ya da kargo kuponu var) "şu kadar daha ekleyin" gösterilmez.
            pricing.ShippingTotal == 0 ? 0 : Math.Max(0, CartPricingCalculator.FreeShippingThreshold - afterDiscount));
    }

    private static string? VariantLabel(string? color, string? size) =>
        (color, size) switch
        {
            (null, null) => null,
            (not null, null) => color,
            (null, not null) => size,
            _ => $"{color} / {size}",
        };

    private static CartDto Empty(string? couponCode = null) =>
        new([], 0, 0, 0, 0, 0, 0, couponCode, null, CartPricingCalculator.FreeShippingThreshold);
}
