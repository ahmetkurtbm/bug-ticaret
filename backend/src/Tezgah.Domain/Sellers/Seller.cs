using Tezgah.Domain.Common;

namespace Tezgah.Domain.Sellers;

public class Seller : AuditableEntity
{
    public long OwnerUserId { get; set; }
    public string StoreName { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string LegalName { get; set; } = default!;
    public string TaxNumber { get; set; } = default!;
    public string TaxOffice { get; set; } = default!;
    public string? Iban { get; set; }
    public string City { get; set; } = default!;
    public SellerStatus Status { get; set; } = SellerStatus.Pending;

    /// <summary>Varsayılan komisyon oranı (yüzde). Kategori bazlı oran varsa o geçerlidir.</summary>
    public decimal CommissionRate { get; set; }

    public decimal Rating { get; set; }
    public int RatingCount { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
}
