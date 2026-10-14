namespace Tezgah.Payments.Domain;

public enum PaymentStatus
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3,
}

public class Payment
{
    public Guid Id { get; set; }
    public long OrderId { get; set; }
    public string OrderNumber { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public int InstallmentCount { get; set; }
    public string CustomerEmail { get; set; } = "";
    public string ReturnUrl { get; set; } = "";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

/// <summary>Giden webhook kutusu (outbox) satırı. <see cref="Id"/> monolite giden EventId'dir.</summary>
public class WebhookDelivery
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public Payment? Payment { get; set; }

    /// <summary>Serileştirilmiş PaymentWebhookEvent. jsonb'de saklanır; okunan metin olduğu gibi imzalanıp gönderilir.</summary>
    public string Payload { get; set; } = "";

    public int AttemptCount { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
