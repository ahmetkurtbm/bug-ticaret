using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tezgah.Application.Campaigns;
using Tezgah.Application.Common.Exceptions;
using Tezgah.Application.Common.Interfaces;
using Tezgah.Application.Common.Models;
using Tezgah.Application.Payments;
using Tezgah.Application.Pricing;
using Tezgah.Contracts.Payments;
using Tezgah.Domain.Campaigns;
using Tezgah.Domain.Catalog;
using Tezgah.Domain.Common;
using Tezgah.Domain.Orders;
using Tezgah.Domain.Payments;
using Tezgah.Domain.Sellers;

namespace Tezgah.Application.Orders;

public class OrderService(
    ITezgahDbContext db,
    CouponValidator coupons,
    IPaymentGateway payments,
    IOptions<OrderOptions> options,
    TimeProvider clock,
    ILogger<OrderService> logger) : IOrderService
{
    private const string PaymentProvider = "tezgah-payments";

    public async Task<PlaceOrderResponse> PlaceOrderAsync(long userId, PlaceOrderRequest request, string? idempotencyKey, CancellationToken ct)
    {
        // Aynı anahtarla ikinci istek (çift tıklama, ağ tekrarı) yeni sipariş açmaz, ilkini döner.
        if (idempotencyKey is not null)
        {
            var existing = await db.Orders.AsNoTracking()
                .Where(o => o.UserId == userId && o.IdempotencyKey == idempotencyKey)
                .Select(o => o.OrderNumber)
                .SingleOrDefaultAsync(ct);
            if (existing is not null)
                return await RetryPaymentAsync(userId, existing, ct);
        }

        var cart = await db.Carts.Include(c => c.Items).SingleOrDefaultAsync(c => c.UserId == userId, ct);
        if (cart is null || cart.Items.Count == 0)
            throw new DomainException("cart_empty", "Sepetiniz boş.");

        var address = await db.Addresses.AsNoTracking().SingleOrDefaultAsync(a => a.Id == request.AddressId && a.UserId == userId, ct)
            ?? throw new NotFoundException("Teslimat adresi bulunamadı.");
        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId, ct);

        var variantIds = cart.Items.Select(i => i.VariantId).ToList();
        var variants = await db.ProductVariants.AsNoTracking()
            .Where(v => variantIds.Contains(v.Id))
            .Select(v => new
            {
                v.Id,
                v.Sku,
                v.Color,
                v.Size,
                v.Price,
                v.VatRate,
                v.IsActive,
                ProductName = v.Product.Name,
                ProductStatus = v.Product.Status,
                v.Product.SellerId,
                SellerStatus = v.Product.Seller.Status,
                SellerCommission = v.Product.Seller.CommissionRate,
            })
            .ToDictionaryAsync(v => v.Id, ct);

        foreach (var item in cart.Items)
        {
            if (!variants.TryGetValue(item.VariantId, out var v) || !v.IsActive || v.ProductStatus != ProductStatus.Active || v.SellerStatus != SellerStatus.Active)
                throw new DomainException("variant_unavailable", "Sepetinizdeki bir ürün artık satışta değil. Sepetinizi kontrol edin.");
        }

        Coupon? coupon = cart.CouponCode is null ? null : await coupons.ValidateAsync(cart.CouponCode, userId, ct);
        var lines = cart.Items.OrderBy(i => i.Id)
            .Select(i => new PricingLine(i.VariantId, variants[i.VariantId].SellerId, variants[i.VariantId].Price, i.Quantity, variants[i.VariantId].VatRate))
            .ToList();
        var pricing = CartPricingCalculator.Calculate(lines, coupon is null
            ? null
            : new CouponRule(coupon.Code, coupon.Type, coupon.Value, coupon.MaxDiscountAmount, coupon.MinOrderAmount, coupon.SellerId));
        if (pricing.CouponError is not null)
            throw new DomainException("coupon_not_applicable", pricing.CouponError);

        var now = clock.GetUtcNow();
        var order = new Order
        {
            OrderNumber = OrderNumber.Temporary(),
            UserId = userId,
            Status = OrderStatus.PendingPayment,
            Subtotal = pricing.Subtotal,
            DiscountTotal = pricing.DiscountTotal,
            ShippingTotal = pricing.ShippingTotal,
            VatTotal = pricing.VatTotal,
            GrandTotal = pricing.GrandTotal,
            CouponCode = coupon?.Code,
            IdempotencyKey = idempotencyKey,
            ShippingAddress = ToOrderAddress(address),
            BillingAddress = ToOrderAddress(address),
            PlacedAt = now,
        };

        await using (var tx = await db.Database.BeginTransactionAsync(ct))
        {
            if (idempotencyKey is not null)
            {
                // Aynı anahtarla eşzamanlı iki istek sırayla girer; ikincisi ilkinin siparişini bulur.
                var lockKey = $"order:{userId}:{idempotencyKey}";
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", ct);
                var existing = await db.Orders.AsNoTracking()
                    .Where(o => o.UserId == userId && o.IdempotencyKey == idempotencyKey)
                    .Select(o => o.OrderNumber)
                    .SingleOrDefaultAsync(ct);
                if (existing is not null)
                {
                    await tx.RollbackAsync(ct);
                    return await RetryPaymentAsync(userId, existing, ct);
                }
            }

            // Stok satırları her zaman aynı sırayla kilitlenir; ters sıralı iki sepet birbirini kilitlemez.
            foreach (var line in lines.OrderBy(l => l.VariantId))
            {
                var v = variants[line.VariantId];
                var stockItemId = await ReserveStockAsync(line.VariantId, line.Quantity, ct);
                var discount = pricing.LineDiscounts[line.VariantId];
                order.Items.Add(new OrderItem
                {
                    SellerId = v.SellerId,
                    VariantId = v.Id,
                    StockItemId = stockItemId,
                    ProductName = v.ProductName,
                    Sku = v.Sku,
                    VariantLabel = VariantLabel(v.Color, v.Size),
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    VatRate = v.VatRate,
                    DiscountAmount = discount,
                    LineTotal = line.Gross - discount,
                    CommissionRate = v.SellerCommission,
                });
            }

            if (coupon is not null)
            {
                // Limit kontrolü ile artırma tek komutta: aynı anda gelen iki sipariş limiti aşamaz.
                var claimed = await db.Coupons
                    .Where(c => c.Id == coupon.Id && (c.MaxUses == null || c.UsedCount < c.MaxUses))
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedCount, c => c.UsedCount + 1), ct);
                if (claimed == 0)
                    throw new DomainException("coupon_exhausted", "Bu kuponun kullanım limiti doldu.");
            }

            order.StatusHistory.Add(new OrderStatusHistory { ToStatus = OrderStatus.PendingPayment, ChangedByUserId = userId, ChangedAt = now });
            db.Orders.Add(order);
            await SaveOrderAsync(ct);

            order.OrderNumber = OrderNumber.For(order.Id, now);
            if (coupon is not null)
                db.CouponUsages.Add(new CouponUsage { CouponId = coupon.Id, UserId = userId, OrderId = order.Id, DiscountAmount = pricing.DiscountTotal, UsedAt = now });

            db.CartItems.RemoveRange(cart.Items);
            cart.CouponCode = null;
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        logger.LogInformation("Sipariş oluşturuldu {OrderNumber} kullanıcı {UserId} tutar {GrandTotal}", order.OrderNumber, userId, order.GrandTotal);
        return await StartPaymentAsync(order.Id, order.OrderNumber, order.GrandTotal, request.InstallmentCount, user.Email, ct);
    }

    public async Task<PlaceOrderResponse> RetryPaymentAsync(long userId, string orderNumber, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking()
            .Where(o => o.UserId == userId && o.OrderNumber == orderNumber)
            .Select(o => new { o.Id, o.OrderNumber, o.Status, o.GrandTotal, o.User.Email })
            .SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("Sipariş bulunamadı.");

        if (order.Status != OrderStatus.PendingPayment)
            return new PlaceOrderResponse(order.OrderNumber, null, null);

        var installments = await db.Payments.AsNoTracking()
            .Where(p => p.OrderId == order.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => (int?)p.InstallmentCount)
            .FirstOrDefaultAsync(ct) ?? 1;
        return await StartPaymentAsync(order.Id, order.OrderNumber, order.GrandTotal, installments, order.Email, ct);
    }

    public async Task<PagedResult<OrderSummaryDto>> ListAsync(long userId, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var query = db.Orders.AsNoTracking().Where(o => o.UserId == userId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(o => o.PlacedAt).ThenByDescending(o => o.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderSummaryDto(
                o.OrderNumber,
                o.Status,
                o.PlacedAt,
                o.GrandTotal,
                o.Items.Sum(i => i.Quantity),
                o.Items
                    .OrderBy(i => i.Id)
                    .Take(4)
                    .Select(i => db.ProductImages
                        .Where(img => img.ProductId == db.ProductVariants.Where(v => v.Id == i.VariantId).Select(v => v.ProductId).FirstOrDefault())
                        .OrderBy(img => img.SortOrder)
                        .Select(img => img.Url)
                        .FirstOrDefault() ?? "")
                    .ToList()))
            .ToListAsync(ct);

        return new PagedResult<OrderSummaryDto>(items, page, pageSize, total);
    }

    public async Task<OrderDetailDto> GetAsync(long userId, string orderNumber, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .SingleOrDefaultAsync(o => o.UserId == userId && o.OrderNumber == orderNumber, ct)
            ?? throw new NotFoundException("Sipariş bulunamadı.");

        var variantIds = order.Items.Select(i => i.VariantId).ToList();
        var products = await db.ProductVariants.AsNoTracking()
            .Where(v => variantIds.Contains(v.Id))
            .Select(v => new
            {
                v.Id,
                v.Product.Slug,
                ImageUrl = v.Product.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
            })
            .ToDictionaryAsync(v => v.Id, ct);

        var items = order.Items.OrderBy(i => i.Id).Select(i =>
        {
            products.TryGetValue(i.VariantId, out var p);
            return new OrderItemDto(i.VariantId, i.ProductName, p?.Slug, i.VariantLabel, p?.ImageUrl, i.Quantity, i.UnitPrice, i.DiscountAmount, i.LineTotal);
        }).ToList();

        var lastPaymentStatus = await db.Payments.AsNoTracking()
            .Where(p => p.OrderId == order.Id)
            .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
            .Select(p => (PaymentStatus?)p.Status)
            .FirstOrDefaultAsync(ct);

        var address = order.ShippingAddress is { } a
            ? new OrderAddressDto(a.FullName, a.Phone, a.City, a.District, a.Line, a.PostalCode)
            : null;

        return new OrderDetailDto(
            order.OrderNumber, order.Status, order.PlacedAt, order.PaidAt,
            order.Subtotal, order.DiscountTotal, order.ShippingTotal, order.VatTotal, order.GrandTotal, order.CouponCode,
            address, items,
            order.StatusHistory.OrderBy(h => h.ChangedAt).Select(h => new OrderStatusEntryDto(h.ToStatus, h.ChangedAt)).ToList(),
            order.Status == OrderStatus.PendingPayment,
            lastPaymentStatus == PaymentStatus.Failed);
    }

    /// <summary>
    /// Stoğu tek bir koşullu UPDATE ile ayırır. "Yeterli mi?" kontrolü ile düşüm aynı komutta olduğu için
    /// aynı anda gelen iki sipariş son ürünü birlikte alamaz.
    /// </summary>
    private async Task<long> ReserveStockAsync(long variantId, int quantity, CancellationToken ct)
    {
        var candidates = await db.StockItems.AsNoTracking()
            .Where(s => s.VariantId == variantId && s.Quantity - s.Reserved >= quantity)
            .OrderByDescending(s => s.Quantity - s.Reserved)
            .Select(s => s.Id)
            .ToListAsync(ct);

        foreach (var stockItemId in candidates)
        {
            var updated = await db.StockItems
                .Where(s => s.Id == stockItemId && s.Quantity - s.Reserved >= quantity)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Reserved, x => x.Reserved + quantity)
                    .SetProperty(x => x.UpdatedAt, clock.GetUtcNow()), ct);
            if (updated == 1) return stockItemId;
        }
        throw new ConflictException("insufficient_stock", "Sepetinizdeki bir ürünün stoğu tükendi. Sepetinizi kontrol edin.");
    }

    private async Task SaveOrderAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("ix_orders_user_id_idempotency_key") == true)
        {
            // İki istek aynı anahtarla aynı anda geldi; diğeri kazandı.
            throw new ConflictException("duplicate_request", "Bu sipariş zaten oluşturuluyor.");
        }
    }

    private async Task<PlaceOrderResponse> StartPaymentAsync(long orderId, string orderNumber, decimal amount, int installments, string email, CancellationToken ct)
    {
        var returnUrl = $"{options.Value.StorefrontBaseUrl.TrimEnd('/')}/siparis/{orderNumber}/sonuc";
        try
        {
            var response = await payments.CreatePaymentAsync(
                new CreatePaymentRequest(orderId, orderNumber, amount, "TRY", installments, email, returnUrl), ct);

            var providerId = response.PaymentId.ToString();
            if (!await db.Payments.AnyAsync(p => p.ProviderPaymentId == providerId, ct))
            {
                db.Payments.Add(new Payment
                {
                    OrderId = orderId,
                    Provider = PaymentProvider,
                    ProviderPaymentId = providerId,
                    Amount = amount,
                    InstallmentCount = installments,
                    Status = PaymentStatus.Pending,
                });
                await db.SaveChangesAsync(ct);
            }
            return new PlaceOrderResponse(orderNumber, response.CheckoutUrl, null);
        }
        catch (PaymentGatewayException ex) when (ex.AlreadyPaid)
        {
            logger.LogWarning("Ödeme servisi siparişi ödenmiş bildirdi, onay olayı bekleniyor {OrderNumber}", orderNumber);
            return new PlaceOrderResponse(orderNumber, null, "Bu siparişin ödemesi alındı. Onay birkaç dakika içinde görünecek; tekrar ödeme yapmayın.");
        }
        catch (PaymentGatewayException ex)
        {
            // Sipariş oluştu ama ödeme başlatılamadı: müşteri "Siparişlerim"den tekrar deneyebilir.
            logger.LogError(ex, "Ödeme başlatılamadı {OrderNumber}", orderNumber);
            return new PlaceOrderResponse(orderNumber, null, "Ödeme şu anda başlatılamadı. Siparişiniz kaydedildi; birkaç dakika sonra Siparişlerim sayfasından tekrar deneyin.");
        }
    }

    private static OrderAddress ToOrderAddress(Domain.Identity.Address a) => new()
    {
        FullName = a.FullName,
        Phone = a.Phone,
        City = a.City,
        District = a.District,
        Line = a.Line,
        PostalCode = a.PostalCode,
    };

    private static string? VariantLabel(string? color, string? size) =>
        (color, size) switch
        {
            (null, null) => null,
            (not null, null) => color,
            (null, not null) => size,
            _ => $"{color} / {size}",
        };
}
