namespace Tezgah.Application.Pricing;

public record PricingLine(long VariantId, long SellerId, decimal UnitPrice, int Quantity, decimal VatRate)
{
    public decimal Gross => UnitPrice * Quantity;
}
