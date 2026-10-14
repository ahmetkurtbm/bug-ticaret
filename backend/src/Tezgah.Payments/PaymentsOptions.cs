namespace Tezgah.Payments;

public class PaymentsOptions
{
    public const string SectionName = "Payments";

    /// <summary>Monolitin POST/GET /api/payments çağrılarında X-Api-Key ile göndereceği anahtar.</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>Webhook gövdesini HMAC-SHA256 ile imzalamak için paylaşılan sır.</summary>
    public string WebhookSecret { get; set; } = "";

    /// <summary>Monolitin webhook adresi.</summary>
    public string WebhookUrl { get; set; } = "";

    /// <summary>CheckoutUrl'i üretmek için dışarıdan görünen taban adres. Boşsa istekteki host kullanılır.</summary>
    public string PublicBaseUrl { get; set; } = "";
}
