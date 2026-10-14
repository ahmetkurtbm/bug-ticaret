namespace Tezgah.Domain.Payments;

/// <summary>İşlenmiş dış olay kimlikleri. Aynı olay tekrar gelirse ikinci kez işlenmez.</summary>
public class ProcessedWebhookEvent
{
    public Guid EventId { get; set; }
    public string Source { get; set; } = default!;
    public DateTimeOffset ProcessedAt { get; set; }
}
