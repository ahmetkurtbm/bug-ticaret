using System.Globalization;
using System.Net;
using System.Text;
using Tezgah.Contracts.Events;

namespace Tezgah.Notifications.Logic;

public record EmailContent(string Subject, string BodyHtml);

/// <summary>Olay → Türkçe e-posta. Saf ve durumsuz; tüm dinamik değerler HTML'e yazılmadan önce encode edilir.</summary>
public static class EmailTemplates
{
    public static readonly CultureInfo Culture = new("tr-TR");
    private static readonly TimeZoneInfo Istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public static string FormatMoney(decimal amount) => amount.ToString("N2", Culture) + " TL";

    public static string FormatDate(DateTimeOffset value) =>
        TimeZoneInfo.ConvertTime(value, Istanbul).ToString("dd.MM.yyyy HH:mm", Culture);

    public static EmailContent OrderPaid(OrderPaidEvent e)
    {
        var rows = new StringBuilder();
        foreach (var line in e.Lines)
        {
            var name = Enc(line.ProductName);
            if (!string.IsNullOrWhiteSpace(line.VariantLabel))
                name += $" <span style=\"color:#6b7280;\">({Enc(line.VariantLabel)})</span>";
            rows.Append("<tr>")
                .Append($"<td style=\"padding:8px;border-bottom:1px solid #e5e7eb;\">{name}</td>")
                .Append($"<td style=\"padding:8px;border-bottom:1px solid #e5e7eb;text-align:center;\">{line.Quantity.ToString(Culture)}</td>")
                .Append($"<td style=\"padding:8px;border-bottom:1px solid #e5e7eb;text-align:right;\">{Enc(FormatMoney(line.LineTotal))}</td>")
                .Append("</tr>");
        }

        var content = $"""
            <p style="margin:0 0 12px;">Merhaba {Enc(e.CustomerName)},</p>
            <p style="margin:0 0 12px;">{Enc(e.OrderNumber)} numaralı siparişinizin ödemesi alındı. Teşekkür ederiz.</p>
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse;font-size:14px;">
              <tr>
                <th style="padding:8px;text-align:left;border-bottom:2px solid #d1d5db;">Ürün</th>
                <th style="padding:8px;text-align:center;border-bottom:2px solid #d1d5db;">Adet</th>
                <th style="padding:8px;text-align:right;border-bottom:2px solid #d1d5db;">Tutar</th>
              </tr>
              {rows}
              <tr>
                <td colspan="2" style="padding:8px;text-align:right;font-weight:bold;">Toplam</td>
                <td style="padding:8px;text-align:right;font-weight:bold;">{Enc(FormatMoney(e.GrandTotal))}</td>
              </tr>
            </table>
            <p style="margin:12px 0 0;color:#6b7280;font-size:12px;">Sipariş zamanı: {Enc(FormatDate(e.OccurredAt))}</p>
            """;
        return new($"Siparişiniz alındı – {CleanHeader(e.OrderNumber)}", Wrap("Siparişiniz alındı", content));
    }

    public static EmailContent OrderShipped(OrderShippedEvent e)
    {
        var content = $"""
            <p style="margin:0 0 12px;">Merhaba {Enc(e.CustomerName)},</p>
            <p style="margin:0 0 12px;">{Enc(e.OrderNumber)} numaralı siparişiniz kargoya verildi.</p>
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse;font-size:14px;">
              <tr>
                <td style="padding:8px;border-bottom:1px solid #e5e7eb;color:#6b7280;">Satıcı</td>
                <td style="padding:8px;border-bottom:1px solid #e5e7eb;">{Enc(e.SellerName)}</td>
              </tr>
              <tr>
                <td style="padding:8px;border-bottom:1px solid #e5e7eb;color:#6b7280;">Kargo firması</td>
                <td style="padding:8px;border-bottom:1px solid #e5e7eb;">{Enc(e.Carrier)}</td>
              </tr>
              <tr>
                <td style="padding:8px;border-bottom:1px solid #e5e7eb;color:#6b7280;">Takip numarası</td>
                <td style="padding:8px;border-bottom:1px solid #e5e7eb;">{Enc(e.TrackingNumber)}</td>
              </tr>
              <tr>
                <td style="padding:8px;color:#6b7280;">Kargoya veriliş</td>
                <td style="padding:8px;">{Enc(FormatDate(e.OccurredAt))}</td>
              </tr>
            </table>
            """;
        return new($"Siparişiniz kargoya verildi – {CleanHeader(e.OrderNumber)}", Wrap("Siparişiniz kargoya verildi", content));
    }

    public static EmailContent SellerApproved(SellerApprovedEvent e)
    {
        var content = $"""
            <p style="margin:0 0 12px;">Merhaba {Enc(e.OwnerName)},</p>
            <p style="margin:0 0 12px;"><strong>{Enc(e.StoreName)}</strong> mağazanız onaylandı. Artık ürün ekleyip satışa başlayabilirsiniz.</p>
            <p style="margin:12px 0 0;color:#6b7280;font-size:12px;">Onay zamanı: {Enc(FormatDate(e.OccurredAt))}</p>
            """;
        return new($"Mağazanız onaylandı – {CleanHeader(e.StoreName)}", Wrap("Mağazanız onaylandı", content));
    }

    private static string Enc(string? value) => WebUtility.HtmlEncode(value ?? "");

    /// <summary>Konu satırı HTML değildir; başlık enjeksiyonuna karşı satır sonları temizlenir.</summary>
    private static string CleanHeader(string? value) =>
        (value ?? "").Replace('\r', ' ').Replace('\n', ' ').Trim();

    private static string Wrap(string title, string content) => $"""
        <!DOCTYPE html>
        <html lang="tr">
        <body style="margin:0;padding:0;background:#f3f4f6;font-family:Arial,Helvetica,sans-serif;color:#111827;">
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f3f4f6;">
            <tr>
              <td align="center" style="padding:24px;">
                <table role="presentation" width="600" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:8px;">
                  <tr>
                    <td style="padding:20px 24px;background:#111827;color:#ffffff;font-size:18px;font-weight:bold;">Tezgah</td>
                  </tr>
                  <tr>
                    <td style="padding:24px;">
                      <h1 style="margin:0 0 16px;font-size:20px;">{Enc(title)}</h1>
                      {content}
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:16px 24px;color:#6b7280;font-size:12px;">Bu e-posta Tezgah tarafından gönderilmiştir.</td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;
}
