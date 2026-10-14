using Microsoft.EntityFrameworkCore;
using Tezgah.Application.Common.Exceptions;
using Tezgah.Application.Common.Interfaces;
using Tezgah.Application.Common.Models;
using Tezgah.Application.Common.Text;
using Tezgah.Domain.Catalog;
using Tezgah.Domain.Common;
using Tezgah.Domain.Inventory;

namespace Tezgah.Application.Sellers;

public interface ISellerCatalogService
{
    Task<PagedResult<SellerProductListItemDto>> ListAsync(string? q, ProductStatus? status, int page, int pageSize, CancellationToken ct);
    Task<SellerProductDetailDto> GetAsync(long productId, CancellationToken ct);
    Task<SellerProductDetailDto> CreateAsync(CreateProductRequest request, CancellationToken ct);
    Task<SellerProductDetailDto> UpdateAsync(long productId, UpdateProductRequest request, CancellationToken ct);
    Task<SellerProductDetailDto> SubmitAsync(long productId, CancellationToken ct);
    Task<SellerProductDetailDto> ArchiveAsync(long productId, CancellationToken ct);
    Task<SellerVariantDto> UpdateVariantAsync(long variantId, UpdateVariantRequest request, CancellationToken ct);
    Task<SellerVariantDto> SetStockAsync(long variantId, SetStockRequest request, CancellationToken ct);
}

public class SellerCatalogService(ITezgahDbContext db, SellerContext seller, ICurrentUser currentUser, TimeProvider clock) : ISellerCatalogService
{
    public const int LowStockThreshold = 5;
    private static readonly decimal[] AllowedVatRates = [0m, 1m, 10m, 20m];

    public async Task<PagedResult<SellerProductListItemDto>> ListAsync(string? q, ProductStatus? status, int page, int pageSize, CancellationToken ct)
    {
        var sellerId = await seller.GetActiveSellerIdAsync(ct);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Products.AsNoTracking().Where(p => p.SellerId == sellerId);
        if (status is not null) query = query.Where(p => p.Status == status);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            query = query.Where(p => p.Name.ToLower().Contains(term) || p.Variants.Any(v => v.Sku.ToLower() == term));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new SellerProductListItemDto(
                p.Id, p.Name, p.Slug,
                p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                p.Category.Name,
                p.Status,
                p.Variants.Count,
                db.StockItems.Where(s => p.Variants.Select(v => v.Id).Contains(s.VariantId)).Sum(s => (int?)(s.Quantity - s.Reserved)) ?? 0,
                p.Variants.Min(v => (decimal?)v.Price) ?? 0,
                p.SoldCount,
                p.CreatedAt))
            .ToListAsync(ct);
        return new PagedResult<SellerProductListItemDto>(items, page, pageSize, total);
    }

    public async Task<SellerProductDetailDto> GetAsync(long productId, CancellationToken ct)
    {
        var sellerId = await seller.GetActiveSellerIdAsync(ct);
        return await LoadAsync(sellerId, productId, ct);
    }

    public async Task<SellerProductDetailDto> CreateAsync(CreateProductRequest request, CancellationToken ct)
    {
        var sellerId = await seller.GetActiveSellerIdAsync(ct);
        await ValidateCategoryAndBrandAsync(request.CategoryId, request.BrandId, ct);
        ValidateVariants(request.Variants);

        var skus = request.Variants.Select(v => v.Sku).ToList();
        var taken = await db.ProductVariants.Where(v => skus.Contains(v.Sku)).Select(v => v.Sku).ToListAsync(ct);
        if (taken.Count > 0)
            throw RequestValidationException.For("Variants", $"Bu SKU'lar başka bir üründe kullanılıyor: {string.Join(", ", taken)}");

        var images = request.ImageUrls.Where(u => !string.IsNullOrWhiteSpace(u)).Select(u => u.Trim()).ToList();
        if (images.Any(u => !Uri.TryCreate(u, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps))
            throw RequestValidationException.For("ImageUrls", "Görsel adresleri https ile başlamalı.");

        var now = clock.GetUtcNow();
        var warehouseId = await GetOrCreateWarehouseAsync(sellerId, ct);
        var product = new Product
        {
            SellerId = sellerId,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            Name = request.Name.Trim(),
            Slug = Guid.NewGuid().ToString("N"), // kimlik belli olunca gerçek slug yazılır
            Description = request.Description?.Trim(),
            Status = request.SubmitForApproval ? ProductStatus.PendingApproval : ProductStatus.Draft,
        };
        for (var i = 0; i < images.Count; i++)
            product.Images.Add(new ProductImage { Url = images[i], SortOrder = i, AltText = product.Name });
        foreach (var v in request.Variants)
        {
            product.Variants.Add(new ProductVariant
            {
                Sku = v.Sku,
                Barcode = v.Barcode,
                Color = v.Color?.Trim(),
                Size = v.Size?.Trim(),
                Price = v.Price,
                ListPrice = v.ListPrice,
                VatRate = v.VatRate,
            });
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        product.Slug = $"{SlugHelper.Slugify(product.Name)}-p{product.Id}";

        var stocks = product.Variants.Zip(request.Variants)
            .Select(x => new StockItem { VariantId = x.First.Id, WarehouseId = warehouseId, Quantity = x.Second.Stock })
            .ToList();
        db.StockItems.AddRange(stocks);
        await db.SaveChangesAsync(ct);
        foreach (var stock in stocks.Where(s => s.Quantity > 0))
            db.StockMovements.Add(new StockMovement { StockItemId = stock.Id, Delta = stock.Quantity, Reason = StockMovementReason.InitialLoad, CreatedByUserId = currentUser.UserId, CreatedAt = now });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await LoadAsync(sellerId, product.Id, ct);
    }

    public async Task<SellerProductDetailDto> UpdateAsync(long productId, UpdateProductRequest request, CancellationToken ct)
    {
        var sellerId = await seller.GetActiveSellerIdAsync(ct);
        var product = await FindOwnedAsync(sellerId, productId, ct);
        if (product.Status == ProductStatus.Archived)
            throw new DomainException("product_archived", "Arşivlenmiş ürün düzenlenemez.");
        await ValidateCategoryAndBrandAsync(request.CategoryId, request.BrandId, ct);

        var contentChanged = product.Name != request.Name.Trim() || product.Description != request.Description?.Trim() || product.CategoryId != request.CategoryId;
        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim();
        product.CategoryId = request.CategoryId;
        product.BrandId = request.BrandId;
        // Yayındaki ürünün içeriği değişirse yeniden onaya gider (reddedilen ürün de düzeltilince).
        if (contentChanged && product.Status is ProductStatus.Active or ProductStatus.Rejected)
            product.Status = ProductStatus.PendingApproval;
        await db.SaveChangesAsync(ct);
        return await LoadAsync(sellerId, productId, ct);
    }

    public async Task<SellerProductDetailDto> SubmitAsync(long productId, CancellationToken ct)
    {
        var sellerId = await seller.GetActiveSellerIdAsync(ct);
        var product = await FindOwnedAsync(sellerId, productId, ct);
        if (product.Status is not (ProductStatus.Draft or ProductStatus.Rejected))
            throw new DomainException("invalid_status", "Yalnızca taslak ya da reddedilmiş ürün onaya gönderilebilir.");
        product.Status = ProductStatus.PendingApproval;
        await db.SaveChangesAsync(ct);
        return await LoadAsync(sellerId, productId, ct);
    }

    public async Task<SellerProductDetailDto> ArchiveAsync(long productId, CancellationToken ct)
    {
        var sellerId = await seller.GetActiveSellerIdAsync(ct);
        var product = await FindOwnedAsync(sellerId, productId, ct);
        product.Status = ProductStatus.Archived;
        await db.SaveChangesAsync(ct);
        return await LoadAsync(sellerId, productId, ct);
    }

    public async Task<SellerVariantDto> UpdateVariantAsync(long variantId, UpdateVariantRequest request, CancellationToken ct)
    {
        var sellerId = await seller.GetActiveSellerIdAsync(ct);
        var variant = await db.ProductVariants.SingleOrDefaultAsync(v => v.Id == variantId && v.Product.SellerId == sellerId, ct)
            ?? throw new NotFoundException("Varyant bulunamadı.");

        if (decimal.Round(request.Price, 2) != request.Price)
            throw RequestValidationException.For("Price", "Fiyat en fazla 2 ondalık basamak içerebilir.");
        if (request.ListPrice is { } list && list <= request.Price)
            throw RequestValidationException.For("ListPrice", "Liste fiyatı satış fiyatından büyük olmalı ya da boş bırakılmalı.");

        if (variant.Price != request.Price)
        {
            db.PriceHistory.Add(new PriceHistory { VariantId = variant.Id, OldPrice = variant.Price, NewPrice = request.Price, ChangedByUserId = currentUser.UserId, ChangedAt = clock.GetUtcNow() });
            variant.Price = request.Price;
        }
        variant.ListPrice = request.ListPrice;
        variant.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
        return await LoadVariantAsync(variant.Id, ct);
    }

    public async Task<SellerVariantDto> SetStockAsync(long variantId, SetStockRequest request, CancellationToken ct)
    {
        var sellerId = await seller.GetActiveSellerIdAsync(ct);
        if (!await db.ProductVariants.AnyAsync(v => v.Id == variantId && v.Product.SellerId == sellerId, ct))
            throw new NotFoundException("Varyant bulunamadı.");

        var warehouseId = await GetOrCreateWarehouseAsync(sellerId, ct);
        var now = clock.GetUtcNow();
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var stock = await db.StockItems.SingleOrDefaultAsync(s => s.VariantId == variantId && s.WarehouseId == warehouseId, ct);
        if (stock is null)
        {
            stock = new StockItem { VariantId = variantId, WarehouseId = warehouseId };
            db.StockItems.Add(stock);
            await db.SaveChangesAsync(ct);
        }

        // Ayrılmış (ödeme bekleyen) adedin altına inilemez; koşul UPDATE'in içinde, aradaki siparişler de hesaba katılır.
        var previous = stock.Quantity;
        var updated = await db.StockItems
            .Where(s => s.Id == stock.Id && s.Reserved <= request.Quantity)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Quantity, request.Quantity).SetProperty(x => x.UpdatedAt, now), ct);
        if (updated == 0)
            throw RequestValidationException.For("Quantity", "Stok, ödeme bekleyen siparişlerde ayrılmış adetten az olamaz.");

        if (request.Quantity != previous)
            db.StockMovements.Add(new StockMovement { StockItemId = stock.Id, Delta = request.Quantity - previous, Reason = StockMovementReason.Adjustment, ReferenceType = request.Note is null ? null : "note", CreatedByUserId = currentUser.UserId, CreatedAt = now });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await LoadVariantAsync(variantId, ct);
    }

    private async Task<long> GetOrCreateWarehouseAsync(long sellerId, CancellationToken ct)
    {
        var id = await db.Warehouses.Where(w => w.SellerId == sellerId && w.IsActive).OrderBy(w => w.Id).Select(w => (long?)w.Id).FirstOrDefaultAsync(ct);
        if (id is { } existing) return existing;
        var city = await db.Sellers.Where(s => s.Id == sellerId).Select(s => s.City).SingleAsync(ct);
        var warehouse = new Warehouse { SellerId = sellerId, Name = "Ana depo", City = city };
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(ct);
        return warehouse.Id;
    }

    private async Task ValidateCategoryAndBrandAsync(long categoryId, long? brandId, CancellationToken ct)
    {
        var category = await db.Categories.AsNoTracking().Where(c => c.Id == categoryId && c.IsActive)
            .Select(c => new { HasChildren = c.Children.Any() }).SingleOrDefaultAsync(ct);
        if (category is null)
            throw RequestValidationException.For("CategoryId", "Kategori bulunamadı.");
        if (category.HasChildren)
            throw RequestValidationException.For("CategoryId", "Ürünler yalnızca en alt kategoriye eklenebilir.");
        if (brandId is { } b && !await db.Brands.AnyAsync(x => x.Id == b, ct))
            throw RequestValidationException.For("BrandId", "Marka bulunamadı.");
    }

    private static void ValidateVariants(List<CreateVariantRequest> variants)
    {
        if (variants.Select(v => v.Sku).Distinct().Count() != variants.Count)
            throw RequestValidationException.For("Variants", "Aynı SKU birden fazla varyantta kullanılamaz.");
        foreach (var v in variants)
        {
            if (!AllowedVatRates.Contains(v.VatRate))
                throw RequestValidationException.For("Variants", "KDV oranı 0, 1, 10 ya da 20 olabilir.");
            if (decimal.Round(v.Price, 2) != v.Price)
                throw RequestValidationException.For("Variants", "Fiyat en fazla 2 ondalık basamak içerebilir.");
            if (v.ListPrice is { } list && list <= v.Price)
                throw RequestValidationException.For("Variants", "Liste fiyatı satış fiyatından büyük olmalı.");
        }
    }

    private async Task<Product> FindOwnedAsync(long sellerId, long productId, CancellationToken ct) =>
        await db.Products.SingleOrDefaultAsync(p => p.Id == productId && p.SellerId == sellerId, ct)
        ?? throw new NotFoundException("Ürün bulunamadı.");

    private async Task<SellerProductDetailDto> LoadAsync(long sellerId, long productId, CancellationToken ct)
    {
        var p = await db.Products.AsNoTracking()
            .Where(x => x.Id == productId && x.SellerId == sellerId)
            .Select(x => new
            {
                x.Id, x.Name, x.Slug, x.Description, x.CategoryId, CategoryName = x.Category.Name, x.BrandId,
                BrandName = x.Brand == null ? null : x.Brand.Name, x.Status,
                Images = x.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).ToList(),
            })
            .SingleOrDefaultAsync(ct) ?? throw new NotFoundException("Ürün bulunamadı.");

        var variants = await VariantQuery(db.ProductVariants.Where(v => v.ProductId == productId).OrderBy(v => v.Id)).ToListAsync(ct);
        return new SellerProductDetailDto(p.Id, p.Name, p.Slug, p.Description, p.CategoryId, p.CategoryName, p.BrandId, p.BrandName, p.Status, p.Images, variants);
    }

    private async Task<SellerVariantDto> LoadVariantAsync(long variantId, CancellationToken ct) =>
        await VariantQuery(db.ProductVariants.Where(v => v.Id == variantId)).SingleAsync(ct);

    private IQueryable<SellerVariantDto> VariantQuery(IQueryable<ProductVariant> source) =>
        source.AsNoTracking().Select(v => new SellerVariantDto(
            v.Id, v.Sku, v.Barcode, v.Color, v.Size, v.Price, v.ListPrice, v.VatRate, v.IsActive,
            db.StockItems.Where(s => s.VariantId == v.Id).Sum(s => (int?)s.Quantity) ?? 0,
            db.StockItems.Where(s => s.VariantId == v.Id).Sum(s => (int?)s.Reserved) ?? 0));
}
