namespace Tezgah.Domain.Messaging;

/// <summary>
/// Yayınlanmayı bekleyen olay. İş değişikliğiyle aynı veritabanı işleminde yazılır; ayrı bir iş
/// RabbitMQ'ya gönderir. Böylece "sipariş ödendi ama olay kayboldu" durumu olmaz.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public int Attempts { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; }
    public string? LastError { get; set; }
}
