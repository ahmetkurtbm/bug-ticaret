using Tezgah.Domain.Common;

namespace Tezgah.Domain.Catalog;

public class ProductVariant : AuditableEntity
{
    public long ProductId { get; set; }
    public Product Product { get; set; } = default!;
    public string Sku { get; set; } = default!;
    public string? Barcode { get; set; }
    public string? Color { get; set; }
    public string? Size { get; set; }

    /// <summary>KDV dahil satış fiyatı.</summary>
    public decimal Price { get; set; }

    /// <summary>Üstü çizili liste fiyatı; indirim yoksa null.</summary>
    public decimal? ListPrice { get; set; }

    /// <summary>KDV oranı yüzde olarak (ör. 20).</summary>
    public decimal VatRate { get; set; }

    public decimal? WeightKg { get; set; }
    public bool IsActive { get; set; } = true;
}
