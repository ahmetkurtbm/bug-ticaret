namespace Tezgah.Application.Pricing;

public static class Money
{
    /// <summary>Kuruşa yuvarlama. Türk vergi uygulamasındaki gibi yarım değer yukarı (0,005 → 0,01).</summary>
    public static decimal Round(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);

    /// <summary>KDV dahil tutarın içindeki KDV.</summary>
    public static decimal VatIncluded(decimal grossAmount, decimal vatRatePercent) =>
        Round(grossAmount - grossAmount / (1 + vatRatePercent / 100m));
}
