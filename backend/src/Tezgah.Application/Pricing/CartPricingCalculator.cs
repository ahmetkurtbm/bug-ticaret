using Tezgah.Domain.Campaigns;

namespace Tezgah.Application.Pricing;

/// <summary>
/// Sepet ve sipariş tutarlarını hesaplar. Fiyatlar KDV dahildir. İndirim satırlara tutarlarıyla
/// orantılı dağıtılır; yuvarlama farkı son satıra yazılır ki toplam kuruşu kuruşuna tutsun.
/// </summary>
public static class CartPricingCalculator
{
    public const decimal FreeShippingThreshold = 500m;
    public const decimal StandardShippingFee = 49.90m;

    public static PricingResult Calculate(IReadOnlyList<PricingLine> lines, CouponRule? coupon)
    {
        var subtotal = Money.Round(lines.Sum(l => l.Gross));
        var lineDiscounts = lines.ToDictionary(l => l.VariantId, _ => 0m);
        string? couponError = null;
        var freeShipping = false;

        if (coupon is not null)
        {
            var eligible = coupon.SellerId is null ? lines : lines.Where(l => l.SellerId == coupon.SellerId).ToList();
            var eligibleTotal = Money.Round(eligible.Sum(l => l.Gross));

            if (eligible.Count == 0)
                couponError = "Bu kupon sepetinizdeki ürünlerde geçerli değil.";
            else if (eligibleTotal < coupon.MinOrderAmount)
                couponError = $"Bu kupon için en az {coupon.MinOrderAmount:0.00} TL tutarında ürün gerekiyor.";
            else if (coupon.Type == CouponType.FreeShipping)
                freeShipping = true;
            else
            {
                var discount = coupon.Type == CouponType.Percentage
                    ? Money.Round(eligibleTotal * coupon.Value / 100m)
                    : Math.Min(coupon.Value, eligibleTotal);
                if (coupon.MaxDiscountAmount is { } max)
                    discount = Math.Min(discount, max);

                Distribute(eligible, eligibleTotal, discount, lineDiscounts);
            }
        }

        var discountTotal = lineDiscounts.Values.Sum();
        var afterDiscount = subtotal - discountTotal;
        var shipping = lines.Count == 0 || freeShipping || afterDiscount >= FreeShippingThreshold ? 0m : StandardShippingFee;

        var vat = lines.Sum(l => Money.VatIncluded(l.Gross - lineDiscounts[l.VariantId], l.VatRate));

        return new PricingResult(subtotal, discountTotal, shipping, vat, afterDiscount + shipping, lineDiscounts, couponError);
    }

    private static void Distribute(IReadOnlyList<PricingLine> eligible, decimal eligibleTotal, decimal discount, Dictionary<long, decimal> target)
    {
        var remaining = discount;
        for (var i = 0; i < eligible.Count; i++)
        {
            var line = eligible[i];
            var share = i == eligible.Count - 1 ? remaining : Money.Round(discount * line.Gross / eligibleTotal);
            share = Math.Min(share, line.Gross);
            target[line.VariantId] = share;
            remaining -= share;
        }
    }
}
