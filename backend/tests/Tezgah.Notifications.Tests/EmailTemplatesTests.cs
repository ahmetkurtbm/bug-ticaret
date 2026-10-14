using Tezgah.Contracts.Events;
using Tezgah.Notifications.Logic;

namespace Tezgah.Notifications.Tests;

public class EmailTemplatesTests
{
    private static readonly DateTimeOffset Utc = new(2026, 3, 10, 21, 30, 0, TimeSpan.Zero);

    private static OrderPaidEvent Paid(string customer = "Ayşe", string product = "Seramik Kupa", string? variant = null) =>
        new(Guid.NewGuid(), 1, "TZ-1001", "ayse@example.com", customer, 1234.5m,
            [new OrderLine(product, variant, 2, 1234.5m)], Utc);

    [Fact]
    public void FormatMoney_UsesTurkishLiraFormat()
    {
        Assert.Equal("1.234,50 TL", EmailTemplates.FormatMoney(1234.5m));
        Assert.Equal("0,00 TL", EmailTemplates.FormatMoney(0m));
    }

    [Fact]
    public void FormatDate_ConvertsToIstanbulTime()
    {
        // 21:30 UTC → 00:30 ertesi gün (UTC+3).
        Assert.Equal("11.03.2026 00:30", EmailTemplates.FormatDate(Utc));
    }

    [Fact]
    public void OrderPaid_SubjectAndBody()
    {
        var mail = EmailTemplates.OrderPaid(Paid(variant: "Mavi"));

        Assert.Equal("Siparişiniz alındı – TZ-1001", mail.Subject);
        Assert.Contains("1.234,50 TL", mail.BodyHtml);
        Assert.Contains("11.03.2026 00:30", mail.BodyHtml);
        Assert.Contains("Seramik Kupa", mail.BodyHtml);
        Assert.Contains("Mavi", mail.BodyHtml);
    }

    [Fact]
    public void OrderPaid_HtmlEncodesDynamicValues()
    {
        var mail = EmailTemplates.OrderPaid(Paid(customer: "<script>alert(1)</script>", product: "A & B <b>", variant: "\"x\""));

        Assert.DoesNotContain("<script>", mail.BodyHtml);
        Assert.DoesNotContain("<b>", mail.BodyHtml);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", mail.BodyHtml);
        Assert.Contains("A &amp; B &lt;b&gt;", mail.BodyHtml);
        Assert.Contains("&quot;x&quot;", mail.BodyHtml);
    }

    [Fact]
    public void OrderShipped_SubjectAndEncodedBody()
    {
        var e = new OrderShippedEvent(Guid.NewGuid(), 1, "TZ-1002", "a@b.com", "Ali", "<i>Satıcı</i>", "Yurtici & Co", "TRK<1>", Utc);

        var mail = EmailTemplates.OrderShipped(e);

        Assert.Equal("Siparişiniz kargoya verildi – TZ-1002", mail.Subject);
        Assert.Contains("&lt;i&gt;Satıcı&lt;/i&gt;", mail.BodyHtml);
        Assert.Contains("Yurtici &amp; Co", mail.BodyHtml);
        Assert.Contains("TRK&lt;1&gt;", mail.BodyHtml);
        Assert.DoesNotContain("<i>", mail.BodyHtml);
    }

    [Fact]
    public void SellerApproved_SubjectAndEncodedBody()
    {
        var e = new SellerApprovedEvent(Guid.NewGuid(), 5, "Guzel <Tezgah>", "o@b.com", "Veli", Utc);

        var mail = EmailTemplates.SellerApproved(e);

        Assert.Equal("Mağazanız onaylandı – Guzel <Tezgah>", mail.Subject);
        Assert.Contains("Guzel &lt;Tezgah&gt;", mail.BodyHtml);
        Assert.DoesNotContain("<Tezgah>", mail.BodyHtml);
    }

    [Fact]
    public void Subject_StripsLineBreaks()
    {
        var e = new SellerApprovedEvent(Guid.NewGuid(), 5, "A\r\nBcc: x@y.com", "o@b.com", "Veli", Utc);

        Assert.DoesNotContain('\n', EmailTemplates.SellerApproved(e).Subject);
    }
}
