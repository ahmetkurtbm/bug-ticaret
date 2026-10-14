using Tezgah.Payments.Domain;
using Tezgah.Payments.Logic;

namespace Tezgah.Payments.Tests;

public class PaymentDecisionTests
{
    [Fact]
    public void Approve_WithValidCode_Succeeds()
    {
        var outcome = PaymentDecision.Decide(100m, "approve", "123456");
        Assert.Equal(new PaymentDecision.Outcome(PaymentStatus.Succeeded, null), outcome);
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("000000")]
    [InlineData(null)]
    public void Cancel_AlwaysFailsWithCancelReason(string? code)
    {
        var outcome = PaymentDecision.Decide(100m, "cancel", code);
        Assert.Equal(new PaymentDecision.Outcome(PaymentStatus.Failed, "Kullanıcı ödemeyi iptal etti"), outcome);
    }

    [Theory]
    [InlineData("000000")]
    [InlineData("")]
    [InlineData(null)]
    public void Approve_WithWrongCode_FailsThreeDs(string? code)
    {
        var outcome = PaymentDecision.Decide(100m, "approve", code);
        Assert.Equal(new PaymentDecision.Outcome(PaymentStatus.Failed, "3D doğrulama başarısız"), outcome);
    }

    [Theory]
    [InlineData("100.13")]
    [InlineData("0.13")]
    [InlineData("1234.13")]
    public void Approve_WithKurus13_FailsInsufficientFunds(string amount)
    {
        var outcome = PaymentDecision.Decide(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), "approve", "123456");
        Assert.Equal(new PaymentDecision.Outcome(PaymentStatus.Failed, "Yetersiz bakiye"), outcome);
    }

    [Theory]
    [InlineData("100.00")]
    [InlineData("13.00")]
    [InlineData("100.31")]
    [InlineData("100.14")]
    public void Approve_WithOtherKurus_Succeeds(string amount)
    {
        var parsed = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);
        var outcome = PaymentDecision.Decide(parsed, "approve", "123456");
        Assert.Equal(PaymentStatus.Succeeded, outcome!.Value.Status);
    }

    [Fact]
    public void WrongCode_TakesPrecedenceOverKurusHook()
    {
        var outcome = PaymentDecision.Decide(100.13m, "approve", "111111");
        Assert.Equal("3D doğrulama başarısız", outcome!.Value.FailureReason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("refund")]
    public void UnknownAction_ReturnsNull(string? action) =>
        Assert.Null(PaymentDecision.Decide(100m, action, "123456"));
}
