using System.Globalization;
using System.Net;
using Tezgah.Payments.Domain;

namespace Tezgah.Payments.Checkout;

/// <summary>Sahte 3D Secure sayfasının HTML'i. Dinamik her değer HtmlEncode'dan geçer.</summary>
public static class CheckoutPage
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");

    public static string Render(Payment p, string returnUrlWithPaymentId, string antiforgeryField, string antiforgeryToken)
    {
        var amount = p.Amount.ToString("C2", Tr);
        var summary = $"""
            <dl>
              <dt>Mağaza</dt><dd>Tezgah</dd>
              <dt>Sipariş no</dt><dd>{E(p.OrderNumber)}</dd>
              <dt>Tutar</dt><dd class="amount">{E(amount)}</dd>
              <dt>Taksit</dt><dd>{E(p.InstallmentCount.ToString(Tr))}</dd>
              <dt>Kart</dt><dd>**** **** **** 4242</dd>
            </dl>
            """;

        string body;
        if (p.Status == PaymentStatus.Pending)
        {
            body = $"""
                {summary}
                <form method="post" action="/pay/{E(p.Id.ToString())}">
                  <input type="hidden" name="{E(antiforgeryField)}" value="{E(antiforgeryToken)}">
                  <label for="code">SMS kodu</label>
                  <input id="code" name="code" inputmode="numeric" autocomplete="one-time-code" minlength="6" maxlength="6" required placeholder="______">
                  <p class="hint">Test kodu: 123456</p>
                  <div class="buttons">
                    <button type="submit" name="action" value="approve" class="primary">Ödemeyi onayla</button>
                    <button type="submit" name="action" value="cancel" formnovalidate>İptal et</button>
                  </div>
                </form>
                """;
        }
        else
        {
            var succeeded = p.Status == PaymentStatus.Succeeded;
            var reason = succeeded || string.IsNullOrEmpty(p.FailureReason)
                ? ""
                : $"<p class=\"reason\">{E(p.FailureReason)}</p>";
            body = $"""
                {summary}
                <p class="state {(succeeded ? "ok" : "fail")}">{(succeeded ? "Ödeme başarılı" : "Ödeme başarısız")}</p>
                {reason}
                <p><a href="{E(returnUrlWithPaymentId)}">Mağazaya dön</a></p>
                """;
        }

        return $$"""
            <!DOCTYPE html>
            <html lang="tr">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>3D Secure doğrulama</title>
              <style>
                body { font-family: system-ui, sans-serif; background: #eef1f5; margin: 0; padding: 2rem 1rem; color: #1b2a3a; }
                main { max-width: 26rem; margin: 0 auto; background: #fff; border-radius: 8px; padding: 1.5rem; box-shadow: 0 2px 12px rgba(0,0,0,.1); }
                h1 { font-size: 1.1rem; margin: 0 0 1rem; border-bottom: 2px solid #0b4f9c; padding-bottom: .5rem; color: #0b4f9c; }
                dl { display: grid; grid-template-columns: auto 1fr; gap: .4rem 1rem; margin: 0 0 1.2rem; }
                dt { color: #667; } dd { margin: 0; text-align: right; }
                .amount { font-weight: 700; }
                label { display: block; margin-bottom: .3rem; }
                input[name=code] { width: 100%; box-sizing: border-box; font-size: 1.3rem; letter-spacing: .4rem; text-align: center; padding: .5rem; }
                .hint { color: #667; font-size: .85rem; margin: .3rem 0 1rem; }
                .buttons { display: flex; gap: .6rem; }
                button { flex: 1; padding: .7rem; border: 1px solid #99a; background: #fff; border-radius: 6px; font-size: 1rem; cursor: pointer; }
                button.primary { background: #0b4f9c; border-color: #0b4f9c; color: #fff; }
                .state { font-weight: 700; } .ok { color: #1a7f37; } .fail { color: #b42318; }
              </style>
            </head>
            <body>
              <main>
                <h1>3D Secure doğrulama</h1>
                {{body}}
              </main>
            </body>
            </html>
            """;
    }
}
