using System.Globalization;
using System.Net;
using System.Text;
using Tezgah.Payments.Domain;
using Tezgah.Payments.Logic;

namespace Tezgah.Payments.Checkout;

/// <summary>Panelde gösterilen webhook teslim özeti (yük ve müşteri bilgisi içermez).</summary>
public record PanelDelivery(DateTimeOffset? DeliveredAt, int AttemptCount, string? LastError);

/// <summary>Panel satırı. Müşteri e-postası bilerek yok.</summary>
public record PanelRow(
    Guid Id,
    DateTimeOffset CreatedAt,
    string OrderNumber,
    decimal Amount,
    int InstallmentCount,
    PaymentStatus Status,
    string? FailureReason,
    PanelDelivery? Delivery);

/// <summary>Sahte bankanın "Son ödemeler" paneli. Dinamik her değer HtmlEncode'dan geçer.</summary>
public static class PaymentsPanelPage
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly TimeZoneInfo Istanbul = FindIstanbul();

    // Konteynerde tzdata yoksa sabit UTC+3'e düşer (Türkiye'de yaz saati uygulaması yok).
    private static TimeZoneInfo FindIstanbul()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.CreateCustomTimeZone("TRT", TimeSpan.FromHours(3), "Türkiye", "Türkiye"); }
        catch (InvalidTimeZoneException) { return TimeZoneInfo.CreateCustomTimeZone("TRT", TimeSpan.FromHours(3), "Türkiye", "Türkiye"); }
    }

    private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");

    public static string StatusLabel(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "bekliyor",
        PaymentStatus.Succeeded => "onaylandı",
        PaymentStatus.Failed => "reddedildi",
        _ => "bilinmiyor",
    };

    public static string DeliveryLabel(PanelDelivery? d) => d switch
    {
        null => "",
        { DeliveredAt: not null } => "gönderildi",
        _ when !WebhookBackoff.CanRetry(d.AttemptCount) => "başarısız",
        _ => $"bekliyor ({d.AttemptCount} deneme)",
    };

    public static string Render(IReadOnlyList<PanelRow> rows)
    {
        var sb = new StringBuilder();
        foreach (var r in rows)
        {
            var time = TimeZoneInfo.ConvertTime(r.CreatedAt, Istanbul).ToString("dd.MM.yyyy HH:mm:ss", Tr);
            var cls = r.Status switch
            {
                PaymentStatus.Succeeded => "ok",
                PaymentStatus.Failed => "fail",
                _ => "wait",
            };

            var statusCell = $"<span class=\"badge {cls}\">{E(StatusLabel(r.Status))}</span>";
            if (r.Status == PaymentStatus.Pending)
                statusCell += $" <a href=\"/pay/{E(r.Id.ToString())}\">Ödeme sayfasını aç</a>";
            if (r.Status != PaymentStatus.Succeeded && !string.IsNullOrEmpty(r.FailureReason))
                statusCell += $"<div class=\"reason\">{E(r.FailureReason)}</div>";

            var deliveryCell = "";
            if (r.Delivery is not null)
            {
                var label = DeliveryLabel(r.Delivery);
                deliveryCell = $"bildirim: {E(label)}";
                if (!string.IsNullOrEmpty(r.Delivery.LastError))
                    deliveryCell += $"<div class=\"reason\">{E(r.Delivery.LastError)}</div>";
            }

            sb.Append($"""
                <tr>
                  <td>{E(time)}</td>
                  <td>{E(r.OrderNumber)}</td>
                  <td class="num">{E(r.Amount.ToString("C2", Tr))}</td>
                  <td class="num">{E(r.InstallmentCount.ToString(Tr))}</td>
                  <td>{statusCell}</td>
                  <td>{deliveryCell}</td>
                </tr>

                """);
        }

        var tableBody = rows.Count == 0
            ? "<tr><td colspan=\"6\" class=\"empty\">Henüz ödeme yok.</td></tr>"
            : sb.ToString();

        return $$"""
            <!DOCTYPE html>
            <html lang="tr">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta http-equiv="refresh" content="5">
              <title>Tezgah Ödeme · sahte banka paneli</title>
              <style>
                body { font-family: system-ui, sans-serif; background: #eef1f5; margin: 0; padding: 2rem 1rem; color: #1b2a3a; }
                main { max-width: 64rem; margin: 0 auto; background: #fff; border-radius: 8px; padding: 1.5rem; box-shadow: 0 2px 12px rgba(0,0,0,.1); }
                h1 { font-size: 1.1rem; margin: 0 0 1rem; border-bottom: 2px solid #0b4f9c; padding-bottom: .5rem; color: #0b4f9c; }
                p.intro { color: #445; font-size: .9rem; margin: 0 0 1rem; }
                table { border-collapse: collapse; width: 100%; font-size: .9rem; }
                th, td { text-align: left; padding: .4rem .6rem; border-bottom: 1px solid #e3e6ea; vertical-align: top; }
                th { background: #eef1f5; }
                td.num { text-align: right; font-variant-numeric: tabular-nums; }
                .badge { display: inline-block; padding: .05rem .5rem; border-radius: 4px; color: #fff; font-weight: 600; }
                .ok { background: #1a7f37; } .fail { background: #b42318; } .wait { background: #6b7280; }
                .reason { color: #b42318; font-size: .8rem; margin-top: .2rem; }
                .empty { color: #667; text-align: center; }
              </style>
            </head>
            <body>
              <main>
                <h1>Tezgah Ödeme · sahte banka paneli</h1>
                <p class="intro">Müşteri sitesinden 'Ödemeye geç' denince ödeme burada oluşur. Test SMS kodu 123456; kuruşu ,13 ile biten tutarlar reddedilir.</p>
                <table>
                  <thead>
                    <tr><th>Zaman</th><th>Sipariş no</th><th>Tutar</th><th>Taksit</th><th>Durum</th><th>Webhook</th></tr>
                  </thead>
                  <tbody>
                {{tableBody}}
                  </tbody>
                </table>
              </main>
            </body>
            </html>
            """;
    }
}
