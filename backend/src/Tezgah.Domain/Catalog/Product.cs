using Tezgah.Domain.Common;
using Tezgah.Domain.Sellers;

namespace Tezgah.Domain.Catalog;

public class Product : AuditableEntity
{
    public long SellerId { get; set; }
    public Seller Seller { get; set; } = default!;
    public long CategoryId { get; set; }
    public Category Category { get; set; } = default!;
    public long? BrandId { get; set; }
    public Brand? Brand { get; set; }

    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;

    /// <summary>Satıcının girdiği HTML açıklama.</summary>
    public string? Description { get; set; }

    public ProductStatus Status { get; set; } = ProductStatus.Draft;
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }
    public int SoldCount { get; set; }

    public List<ProductVariant> Variants { get; set; } = new();
    public List<ProductImage> Images { get; set; } = new();
    public List<ProductAttributeValue> Attributes { get; set; } = new();
}
