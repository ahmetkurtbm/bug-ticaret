using Tezgah.Domain.Common;

namespace Tezgah.Domain.Campaigns;

public class Coupon : AuditableEntity
{
    public string Code { get; set; } = default!;
    public string Description { get; set; } = default!;
    public CouponType Type { get; set; }

    /// <summary>Yüzde ise 0-100 arası, sabit tutarsa TL.</summary>
    public decimal Value { get; set; }

    public decimal? MaxDiscountAmount { get; set; }
    public decimal MinOrderAmount { get; set; }
    public int? MaxUses { get; set; }
    public int? MaxUsesPerUser { get; set; }
    public int UsedCount { get; set; }

    /// <summary>Sadece bu satıcının ürünlerinde geçerli; null ise tüm site.</summary>
    public long? SellerId { get; set; }

    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public bool IsActive { get; set; } = true;
}
