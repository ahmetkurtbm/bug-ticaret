namespace Tezgah.Payments.Logic;

public static class WebhookBackoff
{
    public const int MaxAttempts = 20;

    private static readonly TimeSpan[] Schedule =
    [
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(60),
        TimeSpan.FromMinutes(5),
    ];

    private static readonly TimeSpan Tail = TimeSpan.FromMinutes(15);

    /// <summary>Başarısız olan <paramref name="failedAttemptCount"/>. denemeden (1'den başlar) sonraki bekleme süresi.</summary>
    public static TimeSpan GetDelay(int failedAttemptCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(failedAttemptCount, 1);
        return failedAttemptCount <= Schedule.Length ? Schedule[failedAttemptCount - 1] : Tail;
    }

    public static bool CanRetry(int attemptCount) => attemptCount < MaxAttempts;
}
