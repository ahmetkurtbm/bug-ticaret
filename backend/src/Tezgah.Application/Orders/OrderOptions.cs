namespace Tezgah.Application.Orders;

public class OrderOptions
{
    public const string SectionName = "Orders";

    /// <summary>Ödeme yapılmayan sipariş bu süreden sonra iptal edilir ve stok geri bırakılır.</summary>
    public int PaymentTimeoutMinutes { get; set; } = 30;

    /// <summary>Ödeme sonrası dönülen müşteri sitesi adresi.</summary>
    public string StorefrontBaseUrl { get; set; } = default!;
}
