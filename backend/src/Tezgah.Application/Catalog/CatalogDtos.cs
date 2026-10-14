namespace Tezgah.Application.Catalog;

public record CategoryNodeDto(long Id, string Name, string Slug, IReadOnlyList<CategoryNodeDto> Children);

public record ProductCardDto(
    long Id,
    string Name,
    string Slug,
    string? BrandName,
    string SellerName,
    decimal Price,
    decimal? ListPrice,
    string? ImageUrl,
    decimal Rating,
    int ReviewCount);

public class ProductSearchQuery
{
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public string? Q { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    /// <summary>popular | price_asc | price_desc | newest | rating</summary>
    public string Sort { get; set; } = "popular";

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 24;
}

public record BreadcrumbDto(string Name, string Slug);

public record VariantDto(
    long Id,
    string Sku,
    string? Color,
    string? Size,
    decimal Price,
    decimal? ListPrice,
    int AvailableQuantity);

public record ProductAttributeDto(string Name, string Value, string? Unit);

public record SellerSummaryDto(long Id, string StoreName, string Slug, decimal Rating, int RatingCount);

public record ProductDetailDto(
    long Id,
    string Name,
    string Slug,
    string? Description,
    string? BrandName,
    SellerSummaryDto Seller,
    IReadOnlyList<BreadcrumbDto> Breadcrumbs,
    IReadOnlyList<string> Images,
    IReadOnlyList<VariantDto> Variants,
    IReadOnlyList<ProductAttributeDto> Attributes,
    decimal Rating,
    int ReviewCount);
