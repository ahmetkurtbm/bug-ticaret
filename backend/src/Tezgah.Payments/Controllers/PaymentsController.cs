using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tezgah.Contracts.Payments;
using Tezgah.Payments.Domain;
using Tezgah.Payments.Persistence;

namespace Tezgah.Payments.Controllers;

/// <summary>GET /api/payments/{id} yanıtı. Durum: pending | succeeded | failed.</summary>
public record PaymentStatusResponse(
    Guid PaymentId,
    string Status,
    decimal Amount,
    string Currency,
    long OrderId,
    string OrderNumber,
    string? FailureReason);

[ApiController]
[Route("api/payments")]
[ServiceFilter(typeof(ApiKeyFilter))]
public class PaymentsController(
    PaymentsDbContext db,
    IOptions<PaymentsOptions> options,
    TimeProvider timeProvider,
    ILogger<PaymentsController> logger) : ControllerBase
{
    private static readonly int[] AllowedInstallments = [1, 2, 3, 6, 9, 12];

    [HttpPost]
    public async Task<ActionResult<CreatePaymentResponse>> Create(CreatePaymentRequest request, CancellationToken ct)
    {
        Validate(request);
        if (!ModelState.IsValid)
            return ValidationProblem();

        // Aynı sipariş için eşzamanlı istekler sırayla işlenir (işlem sonunda kilit bırakılır).
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({request.OrderId})", ct);

        // Başarılı ödemesi olan siparişe ikinci ödeme açılmaz: geciken webhook beklenirken
        // müşterinin tekrar ödemeye basması çift tahsilat olurdu.
        if (await db.Payments.AnyAsync(p => p.OrderId == request.OrderId && p.Status == PaymentStatus.Succeeded, ct))
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Bu siparişin ödemesi zaten alındı.",
                extensions: new Dictionary<string, object?> { ["code"] = "already_paid" });

        // Idempotent: aynı sipariş için bekleyen ödeme varsa yenisini açmadan onu döndür.
        var payment = await db.Payments
            .Where(p => p.OrderId == request.OrderId && p.Status == PaymentStatus.Pending)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (payment is null)
        {
            payment = new Payment
            {
                Id = Guid.CreateVersion7(),
                OrderId = request.OrderId,
                OrderNumber = request.OrderNumber,
                Amount = request.Amount,
                Currency = request.Currency,
                InstallmentCount = request.InstallmentCount,
                CustomerEmail = request.CustomerEmail,
                ReturnUrl = request.ReturnUrl,
                Status = PaymentStatus.Pending,
                CreatedAt = timeProvider.GetUtcNow(),
            };
            db.Payments.Add(payment);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Ödeme oluşturuldu. PaymentId={PaymentId} OrderId={OrderId} Amount={Amount}",
                payment.Id, payment.OrderId, payment.Amount);
        }
        else
        {
            logger.LogInformation("Bekleyen ödeme yeniden kullanıldı. PaymentId={PaymentId} OrderId={OrderId}",
                payment.Id, payment.OrderId);
        }

        await tx.CommitAsync(ct);
        var baseUrl = string.IsNullOrWhiteSpace(options.Value.PublicBaseUrl)
            ? $"{Request.Scheme}://{Request.Host}"
            : options.Value.PublicBaseUrl.TrimEnd('/');
        return Ok(new CreatePaymentResponse(payment.Id, $"{baseUrl}/pay/{payment.Id}"));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentStatusResponse>> Get(Guid id, CancellationToken ct)
    {
        var p = await db.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Ödeme bulunamadı");

        return new PaymentStatusResponse(p.Id, p.Status.ToString().ToLowerInvariant(), p.Amount, p.Currency,
            p.OrderId, p.OrderNumber, p.FailureReason);
    }

    private void Validate(CreatePaymentRequest r)
    {
        if (r.Amount <= 0)
            ModelState.AddModelError(nameof(r.Amount), "Tutar sıfırdan büyük olmalı.");
        if (r.Currency != "TRY")
            ModelState.AddModelError(nameof(r.Currency), "Yalnızca TRY desteklenir.");
        if (!AllowedInstallments.Contains(r.InstallmentCount))
            ModelState.AddModelError(nameof(r.InstallmentCount), "Taksit sayısı 1, 2, 3, 6, 9 veya 12 olmalı.");
        if (string.IsNullOrWhiteSpace(r.OrderNumber) || r.OrderNumber.Length > 20)
            ModelState.AddModelError(nameof(r.OrderNumber), "Sipariş numarası 1-20 karakter olmalı.");
        if (string.IsNullOrWhiteSpace(r.CustomerEmail) || r.CustomerEmail.Length > 256)
            ModelState.AddModelError(nameof(r.CustomerEmail), "E-posta 1-256 karakter olmalı.");
        if (string.IsNullOrWhiteSpace(r.ReturnUrl) || r.ReturnUrl.Length > 500 ||
            !Uri.TryCreate(r.ReturnUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            ModelState.AddModelError(nameof(r.ReturnUrl), "Dönüş adresi mutlak bir http(s) adresi olmalı (en fazla 500 karakter).");
    }
}
