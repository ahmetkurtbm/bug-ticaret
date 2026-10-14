using Tezgah.Domain.Common;

namespace Tezgah.Domain.Campaigns;

public class CouponUsage : Entity
{
    public long CouponId { get; set; }
    public long UserId { get; set; }
    public long OrderId { get; set; }
    public decimal DiscountAmount { get; set; }
    public DateTimeOffset UsedAt { get; set; }
}
