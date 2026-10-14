namespace Tezgah.Infrastructure.Payments;

public class PaymentsOptions
{
    public const string SectionName = "Payments";

    public string BaseUrl { get; set; } = default!;
    public string ApiKey { get; set; } = default!;
    public string WebhookSecret { get; set; } = default!;
    public int TimeoutSeconds { get; set; } = 10;
}
