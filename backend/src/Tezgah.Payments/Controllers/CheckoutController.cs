using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Tezgah.Contracts.Payments;
using Tezgah.Payments.Checkout;
using Tezgah.Payments.Domain;
using Tezgah.Payments.Logic;
using Tezgah.Payments.Persistence;

namespace Tezgah.Payments.Controllers;

/// <summary>Müşterinin gördüğü sahte 3D Secure sayfası (herkese açık; API anahtarı gerekmez).</summary>
[Route("pay")]
public class CheckoutController(
    PaymentsDbContext db,
    IAntiforgery antiforgery,
    TimeProvider timeProvider,
    ILogger<CheckoutController> logger) : Controller
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Show(Guid id, CancellationToken ct)
    {
        var payment = await db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (payment is null)
            return NotFound();

        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        var html = CheckoutPage.Render(payment, ReturnUrlFor(payment), tokens.FormFieldName, tokens.RequestToken ?? "");
        return Content(html, "text/html; charset=utf-8");
    }

    [HttpPost("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid id, [FromForm] string? code, [FromForm] string? action, CancellationToken ct)
    {
        // Durum değişikliği ve outbox satırı tek transaction'da; ikisi birden ya olur ya olmaz.
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Satırı kilitle: aynı formun çift gönderilmesi iki webhook üretmesin.
        var payment = (await db.Payments
            .FromSqlInterpolated($"select * from payments where id = {id} for update")
            .ToListAsync(ct)).FirstOrDefault();
        if (payment is null)
            return NotFound();

        if (payment.Status == PaymentStatus.Pending)
        {
            var outcome = PaymentDecision.Decide(payment.Amount, action, code);
            if (outcome is null)
                return BadRequest("Geçersiz işlem.");

            var now = timeProvider.GetUtcNow();
            payment.Status = outcome.Value.Status;
            payment.FailureReason = outcome.Value.FailureReason;
            payment.CompletedAt = now;

            var eventId = Guid.CreateVersion7();
            var webhookEvent = new PaymentWebhookEvent(
                eventId, payment.Id, payment.OrderId, payment.OrderNumber,
                payment.Status == PaymentStatus.Succeeded ? PaymentEventStatus.Succeeded : PaymentEventStatus.Failed,
                payment.Amount, payment.Currency, payment.InstallmentCount, payment.FailureReason, now);

            db.WebhookDeliveries.Add(new WebhookDelivery
            {
                Id = eventId,
                PaymentId = payment.Id,
                Payload = JsonSerializer.Serialize(webhookEvent, JsonSerializerOptions.Web),
                AttemptCount = 0,
                NextAttemptAt = now,
                CreatedAt = now,
            });

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            logger.LogInformation("Ödeme tamamlandı. PaymentId={PaymentId} Status={Status} EventId={EventId}",
                payment.Id, payment.Status, eventId);
        }

        return Redirect303(ReturnUrlFor(payment));
    }

    private static string ReturnUrlFor(Payment p) =>
        QueryHelpers.AddQueryString(p.ReturnUrl, "paymentId", p.Id.ToString());

    // RedirectResult 302/301/307/308 üretir; POST sonrası GET'e dönmek için 303 gerekir.
    private static SeeOtherResult Redirect303(string url) => new(url);

    private sealed class SeeOtherResult(string url) : IActionResult
    {
        public Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status303SeeOther;
            context.HttpContext.Response.Headers.Location = url;
            return Task.CompletedTask;
        }
    }
}
