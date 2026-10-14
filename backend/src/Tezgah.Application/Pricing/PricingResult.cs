namespace Tezgah.Application.Pricing;

public record PricingResult(
    decimal Subtotal,
    decimal DiscountTotal,
    decimal ShippingTotal,
    decimal VatTotal,
    decimal GrandTotal,
    IReadOnlyDictionary<long, decimal> LineDiscounts,
    string? CouponError);
