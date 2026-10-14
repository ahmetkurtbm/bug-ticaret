using Tezgah.Payments.Logic;

namespace Tezgah.Payments.Tests;

public class WebhookBackoffTests
{
    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 5)]
    [InlineData(3, 15)]
    [InlineData(4, 60)]
    [InlineData(5, 300)]
    [InlineData(6, 900)]
    [InlineData(19, 900)]
    [InlineData(20, 900)]
    public void GetDelay_FollowsSchedule(int failedAttempt, int expectedSeconds) =>
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), WebhookBackoff.GetDelay(failedAttempt));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetDelay_RejectsNonPositive(int attempt) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => WebhookBackoff.GetDelay(attempt));

    [Theory]
    [InlineData(0, true)]
    [InlineData(19, true)]
    [InlineData(20, false)]
    [InlineData(21, false)]
    public void CanRetry_StopsAtMaxAttempts(int attemptCount, bool expected) =>
        Assert.Equal(expected, WebhookBackoff.CanRetry(attemptCount));
}
