using Tezgah.Application.Pricing;
using Tezgah.Domain.Campaigns;

namespace Tezgah.UnitTests.Pricing;

public class CartPricingCalculatorTests
{
    private static PricingLine Line(long variantId, decimal price, int qty = 1, long sellerId = 1, decimal vat = 20) =>
        new(variantId, sellerId, price, qty, vat);

    [Fact]
    public void Empty_cart_has_no_shipping_fee()
    {
        var result = CartPricingCalculator.Calculate([], null);

        Assert.Equal(0, result.GrandTotal);
        Assert.Equal(0, result.ShippingTotal);
    }

    [Fact]
    public void Shipping_fee_applies_below_threshold_and_is_free_at_threshold()
    {
        var below = CartPricingCalculator.Calculate([Line(1, 499.99m)], null);
        var at = CartPricingCalculator.Calculate([Line(1, 500m)], null);

        Assert.Equal(CartPricingCalculator.StandardShippingFee, below.ShippingTotal);
        Assert.Equal(499.99m + CartPricingCalculator.StandardShippingFee, below.GrandTotal);
        Assert.Equal(0, at.ShippingTotal);
    }

    [Fact]
    public void Percentage_discount_is_distributed_and_sums_exactly()
    {
        // 3 satıra %10: 33,33 + 33,33 + 33,34 yuvarlama farkı son satırda kalmalı
        var lines = new[] { Line(1, 333.33m), Line(2, 333.33m), Line(3, 333.34m) };
        var coupon = new CouponRule("TEST10", CouponType.Percentage, 10, null, 0, null);

        var result = CartPricingCalculator.Calculate(lines, coupon);

        Assert.Equal(100.00m, result.DiscountTotal);
        Assert.Equal(result.DiscountTotal, result.LineDiscounts.Values.Sum());
        Assert.Equal(900.00m, result.GrandTotal);
    }

    [Fact]
    public void Max_discount_caps_percentage_coupon()
    {
        var coupon = new CouponRule("CAP", CouponType.Percentage, 50, MaxDiscountAmount: 100, MinOrderAmount: 0, SellerId: null);

        var result = CartPricingCalculator.Calculate([Line(1, 1000m)], coupon);

        Assert.Equal(100m, result.DiscountTotal);
    }

    [Fact]
    public void Seller_coupon_only_discounts_that_sellers_lines()
    {
        var coupon = new CouponRule("SATICI", CouponType.FixedAmount, 50, null, 0, SellerId: 7);

        var result = CartPricingCalculator.Calculate([Line(1, 300m, sellerId: 7), Line(2, 300m, sellerId: 8)], coupon);

        Assert.Equal(50m, result.LineDiscounts[1]);
        Assert.Equal(0m, result.LineDiscounts[2]);
    }

    [Fact]
    public void Min_order_amount_not_met_returns_error_without_discount()
    {
        var coupon = new CouponRule("MIN", CouponType.FixedAmount, 100, null, MinOrderAmount: 750, SellerId: null);

        var result = CartPricingCalculator.Calculate([Line(1, 600m)], coupon);

        Assert.NotNull(result.CouponError);
        Assert.Equal(0, result.DiscountTotal);
    }

    [Fact]
    public void Fixed_discount_never_exceeds_cart_total()
    {
        var coupon = new CouponRule("BIG", CouponType.FixedAmount, 500, null, 0, null);

        var result = CartPricingCalculator.Calculate([Line(1, 120m)], coupon);

        Assert.Equal(120m, result.DiscountTotal);
        Assert.True(result.GrandTotal >= 0);
    }

    [Theory]
    [InlineData(120, 20, 20.00)]
    [InlineData(110, 10, 10.00)]
    [InlineData(99.99, 20, 16.67)]
    public void Vat_is_extracted_from_gross_price(decimal gross, decimal rate, decimal expectedVat)
    {
        Assert.Equal(expectedVat, Money.VatIncluded(gross, rate));
    }
}
