using Tezgah.Domain.Campaigns;

namespace Tezgah.Application.Pricing;

public record CouponRule(
    string Code,
    CouponType Type,
    decimal Value,
    decimal? MaxDiscountAmount,
    decimal MinOrderAmount,
    long? SellerId);
