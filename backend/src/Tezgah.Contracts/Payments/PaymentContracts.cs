using System.Security.Cryptography;
using System.Text;

namespace Tezgah.Contracts.Payments;

// Monolit (Tezgah.Api) ile Ödeme servisi (Tezgah.Payments) arasındaki sözleşme.
// Alan adı ya da anlamı değişirse iki taraf birlikte güncellenmeli; sadece alan eklemek geriye uyumludur.

/// <summary>Monolit → Ödeme servisi: POST /api/payments (başlık: X-Api-Key).</summary>
public record CreatePaymentRequest(
    long OrderId,
    string OrderNumber,
    decimal Amount,
    string Currency,
    int InstallmentCount,
    string CustomerEmail,
    string ReturnUrl);

/// <summary>Müşteri <see cref="CheckoutUrl"/> adresine yönlendirilir (sahte 3D sayfası).</summary>
public record CreatePaymentResponse(Guid PaymentId, string CheckoutUrl);

public static class PaymentEventStatus
{
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
}

/// <summary>
/// Ödeme servisi → Monolit: POST /api/payments/webhook. En az bir kez teslim edilir; aynı
/// <see cref="EventId"/> birden fazla kez gelebilir, alıcı tekrarı yok saymalıdır.
/// </summary>
public record PaymentWebhookEvent(
    Guid EventId,
    Guid PaymentId,
    long OrderId,
    string OrderNumber,
    string Status,
    decimal Amount,
    string Currency,
    int InstallmentCount,
    string? FailureReason,
    DateTimeOffset OccurredAt);

public static class PaymentWebhookSignature
{
    public const string Header = "X-Tezgah-Signature";
    public const string ApiKeyHeader = "X-Api-Key";

    /// <summary>Ham istek gövdesinin HMAC-SHA256 imzası, küçük harf hex.</summary>
    public static string Sign(string body, string secret) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(body)));

    public static bool Verify(string body, string secret, string? signature) =>
        signature is not null &&
        CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Sign(body, secret)), Encoding.ASCII.GetBytes(signature));
}
