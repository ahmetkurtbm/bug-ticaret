using Microsoft.EntityFrameworkCore;
using Tezgah.Application.Common.Exceptions;
using Tezgah.Application.Common.Interfaces;
using Tezgah.Application.Common.Models;
using Tezgah.Domain.Catalog;

namespace Tezgah.Application.Catalog;

public class CatalogService(ITezgahDbContext db) : ICatalogService
{
    public async Task<IReadOnlyList<CategoryNodeDto>> GetCategoryTreeAsync(CancellationToken ct)
    {
        var all = await db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new { c.Id, c.ParentId, c.Name, c.Slug })
            .ToListAsync(ct);

        var byParent = all.ToLookup(c => c.ParentId);
        IReadOnlyList<CategoryNodeDto> Build(long? parentId) =>
            byParent[parentId].Select(c => new CategoryNodeDto(c.Id, c.Name, c.Slug, Build(c.Id))).ToList();

        return Build(null);
    }

    public async Task<PagedResult<ProductCardDto>> SearchAsync(ProductSearchQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 96);

        var products = db.Products.AsNoTracking().Where(p => p.Status == ProductStatus.Active);

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var categoryIds = await GetCategoryWithDescendantsAsync(query.Category, ct);
            products = products.Where(p => categoryIds.Contains(p.CategoryId));
        }
        if (!string.IsNullOrWhiteSpace(query.Brand))
            products = products.Where(p => p.Brand != null && p.Brand.Slug == query.Brand);
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim().ToLowerInvariant();
            products = products.Where(p => p.Name.ToLower().Contains(term));
        }

        var rows = products.Select(p => new
        {
            p.Id,
            p.Name,
            p.Slug,
            BrandName = p.Brand != null ? p.Brand.Name : null,
            SellerName = p.Seller.StoreName,
            Price = p.Variants.Where(v => v.IsActive).Min(v => (decimal?)v.Price),
            ListPrice = p.Variants.Where(v => v.IsActive).Min(v => v.ListPrice),
            ImageUrl = p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
            p.Rating,
            p.ReviewCount,
            p.SoldCount,
            p.CreatedAt,
        }).Where(x => x.Price != null);

        if (query.MinPrice is { } min) rows = rows.Where(x => x.Price >= min);
        if (query.MaxPrice is { } max) rows = rows.Where(x => x.Price <= max);

        rows = query.Sort switch
        {
            "price_asc" => rows.OrderBy(x => x.Price).ThenBy(x => x.Id),
            "price_desc" => rows.OrderByDescending(x => x.Price).ThenBy(x => x.Id),
            "newest" => rows.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id),
            "rating" => rows.OrderByDescending(x => x.Rating).ThenByDescending(x => x.ReviewCount).ThenBy(x => x.Id),
            _ => rows.OrderByDescending(x => x.SoldCount).ThenBy(x => x.Id),
        };

        var total = await rows.CountAsync(ct);
        var items = await rows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ProductCardDto(x.Id, x.Name, x.Slug, x.BrandName, x.SellerName, x.Price!.Value, x.ListPrice, x.ImageUrl, x.Rating, x.ReviewCount))
            .ToListAsync(ct);

        return new PagedResult<ProductCardDto>(items, page, pageSize, total);
    }

    public async Task<ProductDetailDto> GetBySlugAsync(string slug, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking()
            .Where(p => p.Slug == slug && p.Status == ProductStatus.Active)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Slug,
                p.Description,
                BrandName = p.Brand != null ? p.Brand.Name : null,
                Seller = new SellerSummaryDto(p.Seller.Id, p.Seller.StoreName, p.Seller.Slug, p.Seller.Rating, p.Seller.RatingCount),
                p.CategoryId,
                Images = p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).ToList(),
                Attributes = p.Attributes
                    .Select(a => new ProductAttributeDto(a.Attribute.Name, a.Value, a.Attribute.Unit))
                    .ToList(),
                p.Rating,
                p.ReviewCount,
            })
            .SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("Ürün bulunamadı.");

        var variants = await db.ProductVariants.AsNoTracking()
            .Where(v => v.ProductId == product.Id && v.IsActive)
            .OrderBy(v => v.Price).ThenBy(v => v.Id)
            .Select(v => new VariantDto(
                v.Id,
                v.Sku,
                v.Color,
                v.Size,
                v.Price,
                v.ListPrice,
                db.StockItems.Where(s => s.VariantId == v.Id).Sum(s => s.Quantity - s.Reserved)))
            .ToListAsync(ct);

        var breadcrumbs = await GetBreadcrumbsAsync(product.CategoryId, ct);

        return new ProductDetailDto(
            product.Id, product.Name, product.Slug, product.Description, product.BrandName, product.Seller,
            breadcrumbs, product.Images, variants, product.Attributes, product.Rating, product.ReviewCount);
    }

    private async Task<List<long>> GetCategoryWithDescendantsAsync(string slug, CancellationToken ct)
    {
        var all = await db.Categories.AsNoTracking().Select(c => new { c.Id, c.ParentId, c.Slug }).ToListAsync(ct);
        var root = all.SingleOrDefault(c => c.Slug == slug) ?? throw new NotFoundException("Kategori bulunamadı.");

        var byParent = all.ToLookup(c => c.ParentId);
        var result = new List<long>();
        var stack = new Stack<long>([root.Id]);
        while (stack.Count > 0)
        {
            var id = stack.Pop();
            result.Add(id);
            foreach (var child in byParent[id]) stack.Push(child.Id);
        }
        return result;
    }

    private async Task<IReadOnlyList<BreadcrumbDto>> GetBreadcrumbsAsync(long categoryId, CancellationToken ct)
    {
        var all = await db.Categories.AsNoTracking()
            .Select(c => new { c.Id, c.ParentId, c.Name, c.Slug })
            .ToDictionaryAsync(c => c.Id, ct);

        var trail = new List<BreadcrumbDto>();
        long? current = categoryId;
        while (current is { } id && all.TryGetValue(id, out var c))
        {
            trail.Add(new BreadcrumbDto(c.Name, c.Slug));
            current = c.ParentId;
        }
        trail.Reverse();
        return trail;
    }
}
