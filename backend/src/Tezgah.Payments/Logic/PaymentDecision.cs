using Tezgah.Payments.Domain;

namespace Tezgah.Payments.Logic;

/// <summary>3D Secure formunun sonucunu belirleyen saf (DB'siz) karar mantığı.</summary>
public static class PaymentDecision
{
    public const string ApproveAction = "approve";
    public const string CancelAction = "cancel";
    public const string ValidSmsCode = "123456";

    public const string CancelledReason = "Kullanıcı ödemeyi iptal etti";
    public const string ThreeDsFailedReason = "3D doğrulama başarısız";
    public const string InsufficientFundsReason = "Yetersiz bakiye";

    public readonly record struct Outcome(PaymentStatus Status, string? FailureReason);

    /// <summary>
    /// Sırayla: iptal → yanlış SMS kodu → test kancası → onay. Bilinmeyen eylem için null döner.
    /// Test kancası: tutarın kuruş kısmı 13 ise (ör. 100,13) kod doğru olsa bile "Yetersiz bakiye" ile reddedilir.
    /// </summary>
    public static Outcome? Decide(decimal amount, string? action, string? code)
    {
        if (action == CancelAction)
            return new Outcome(PaymentStatus.Failed, CancelledReason);
        if (action != ApproveAction)
            return null;
        if (code != ValidSmsCode)
            return new Outcome(PaymentStatus.Failed, ThreeDsFailedReason);
        if (HasInsufficientFundsKurus(amount))
            return new Outcome(PaymentStatus.Failed, InsufficientFundsReason);
        return new Outcome(PaymentStatus.Succeeded, null);
    }

    public static bool HasInsufficientFundsKurus(decimal amount) =>
        decimal.Truncate(Math.Abs(amount) * 100) % 100 == 13;
}
