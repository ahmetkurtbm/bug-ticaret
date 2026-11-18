using Tezgah.Payments.Checkout;
using Tezgah.Payments.Domain;

namespace Tezgah.Payments.Tests;

public class PaymentsPanelPageTests
{
    private static PanelRow Row(PaymentStatus status, string orderNumber = "TZ-1", string? reason = null, PanelDelivery? delivery = null) =>
        new(Guid.NewGuid(), new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero), orderNumber, 1234.5m, 1, status, reason, delivery);

    [Fact]
    public void Render_HtmlEncodesDynamicValues()
    {
        var html = PaymentsPanelPage.Render([Row(PaymentStatus.Failed, "<script>x</script>", "<b>red</b>")]);

        Assert.DoesNotContain("<script>x</script>", html);
        Assert.DoesNotContain("<b>red</b>", html);
        Assert.Contains("&lt;script&gt;x&lt;/script&gt;", html);
    }

    [Fact]
    public void Render_PendingPaymentLinksToCheckoutPage()
    {
        var row = Row(PaymentStatus.Pending);
        var html = PaymentsPanelPage.Render([row]);

        Assert.Contains($"/pay/{row.Id}", html);
        Assert.Contains("bekliyor", html);
    }

    [Theory]
    [InlineData(PaymentStatus.Pending, "bekliyor")]
    [InlineData(PaymentStatus.Succeeded, "onaylandı")]
    [InlineData(PaymentStatus.Failed, "reddedildi")]
    public void StatusLabel_IsTurkish(PaymentStatus status, string expected) =>
        Assert.Equal(expected, PaymentsPanelPage.StatusLabel(status));

    [Fact]
    public void DeliveryLabel_CoversAllStates()
    {
        var at = DateTimeOffset.UtcNow;
        Assert.Equal("gönderildi", PaymentsPanelPage.DeliveryLabel(new PanelDelivery(at, 1, null)));
        Assert.Equal("bekliyor (3 deneme)", PaymentsPanelPage.DeliveryLabel(new PanelDelivery(null, 3, "hata")));
        Assert.Equal("başarısız", PaymentsPanelPage.DeliveryLabel(new PanelDelivery(null, 20, "hata")));
    }
}
