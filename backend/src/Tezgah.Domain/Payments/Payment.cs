using Tezgah.Domain.Common;

namespace Tezgah.Domain.Payments;

/// <summary>
/// Monolitteki ödeme kaydı. Ödeme işlemleri yeni Ödeme servisine taşınıyor; bu tablo geçiş süresince
/// iki taraftan da yazılıyor.
/// </summary>
public class Payment : AuditableEntity
{
    public long OrderId { get; set; }
    public string Provider { get; set; } = default!;
    public string? ProviderPaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public int InstallmentCount { get; set; } = 1;
    public string? FailureReason { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
