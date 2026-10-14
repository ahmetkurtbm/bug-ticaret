namespace Tezgah.Notifications.Domain;

public static class NotificationStatus
{
    public const string Pending = "pending";
    public const string Sent = "sent";
    public const string Failed = "failed";
}

public class Notification
{
    public Guid Id { get; set; }

    /// <summary>Olayın EventId'si; tekilleştirme anahtarı (UNIQUE).</summary>
    public Guid EventId { get; set; }
    public string EventType { get; set; } = "";
    public string RecipientEmail { get; set; } = "";
    public string Subject { get; set; } = "";
    public string BodyHtml { get; set; } = "";
    public string Status { get; set; } = NotificationStatus.Pending;
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
}
