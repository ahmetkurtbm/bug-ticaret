using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tezgah.Application.Payments;
using Tezgah.Contracts.Payments;
using Tezgah.Infrastructure.Payments;

namespace Tezgah.Api.Controllers;

/// <summary>Ödeme servisinin olay bildirimleri. Kimlik doğrulama token'la değil, gövde imzasıyla yapılır.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/payments/webhook")]
public class PaymentWebhookController(IPaymentWebhookService webhooks, IOptions<PaymentsOptions> options, ILogger<PaymentWebhookController> logger)
    : ControllerBase
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        // İmza ham gövde üzerinden hesaplanır; model bağlama gövdeyi yeniden biçimlendirebileceği için elle okunur.
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);

        if (!PaymentWebhookSignature.Verify(body, options.Value.WebhookSecret, Request.Headers[PaymentWebhookSignature.Header]))
        {
            logger.LogWarning("Geçersiz imzalı ödeme olayı reddedildi");
            return Unauthorized();
        }

        PaymentWebhookEvent? evt;
        try
        {
            evt = JsonSerializer.Deserialize<PaymentWebhookEvent>(body, Json);
        }
        catch (JsonException)
        {
            return BadRequest();
        }
        if (evt is null) return BadRequest();

        await webhooks.HandleAsync(evt, ct);
        // Tekrar ve yok sayılan olaylarda da 200: gönderen yeniden denemeyi bıraksın.
        return Ok();
    }
}
