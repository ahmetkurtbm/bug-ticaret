using System.Net.Http;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tezgah.Contracts.Payments;
using Tezgah.Payments.Logic;
using Tezgah.Payments.Persistence;

namespace Tezgah.Payments.Webhooks;

/// <summary>
/// Outbox'taki webhook'ları monolite gönderir. Teslimat "en az bir kez"dir (at-least-once): monolit olayı işleyip
/// yanıt zaman aşımına uğrarsa aynı EventId yeniden gönderilir; alıcı tekrarı EventId ile yok saymalıdır.
/// </summary>
public class WebhookDeliveryWorker(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    IOptions<PaymentsOptions> options,
    TimeProvider timeProvider,
    ILogger<WebhookDeliveryWorker> logger) : BackgroundService
{
    public const string HttpClientName = "webhook";
    private const int BatchSize = 20;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = 0;
            try
            {
                processed = await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Webhook partisi işlenirken beklenmeyen hata.");
            }

            // Tam parti geldiyse bekleyen daha olabilir; beklemeden devam et.
            if (processed >= BatchSize)
                continue;

            try
            {
                await Task.Delay(PollInterval, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task<int> ProcessBatchAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var settings = options.Value;
        var client = httpClientFactory.CreateClient(HttpClientName);

        await using var tx = await db.Database.BeginTransactionAsync(stoppingToken);

        // SKIP LOCKED: birden fazla örnek aynı satırı aynı anda göndermesin.
        var now = timeProvider.GetUtcNow();
        var batch = await db.WebhookDeliveries
            .FromSqlInterpolated($"""
                select * from webhook_deliveries
                where delivered_at is null and next_attempt_at <= {now} and attempt_count < {WebhookBackoff.MaxAttempts}
                order by next_attempt_at
                limit {BatchSize}
                for update skip locked
                """)
            .ToListAsync(stoppingToken);

        var processed = 0;
        foreach (var delivery in batch)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            string? error = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, settings.WebhookUrl)
                {
                    Content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json"),
                };
                request.Headers.Add(PaymentWebhookSignature.Header,
                    PaymentWebhookSignature.Sign(delivery.Payload, settings.WebhookSecret));

                using var response = await client.SendAsync(request, stoppingToken);
                if (!response.IsSuccessStatusCode)
                    error = $"HTTP {(int)response.StatusCode}";
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            processed++;
            delivery.AttemptCount++;
            var attemptTime = timeProvider.GetUtcNow();
            if (error is null)
            {
                delivery.DeliveredAt = attemptTime;
                delivery.LastError = null;
                logger.LogInformation("Webhook teslim edildi. EventId={EventId} PaymentId={PaymentId} Attempt={Attempt}",
                    delivery.Id, delivery.PaymentId, delivery.AttemptCount);
            }
            else
            {
                delivery.LastError = error.Length > 1000 ? error[..1000] : error;
                delivery.NextAttemptAt = attemptTime + WebhookBackoff.GetDelay(delivery.AttemptCount);
                if (WebhookBackoff.CanRetry(delivery.AttemptCount))
                    logger.LogWarning("Webhook gönderilemedi. EventId={EventId} PaymentId={PaymentId} Attempt={Attempt} Error={Error} NextAttemptAt={NextAttemptAt}",
                        delivery.Id, delivery.PaymentId, delivery.AttemptCount, error, delivery.NextAttemptAt);
                else
                    logger.LogError("Webhook en fazla deneme sayısına ulaştı, vazgeçildi. EventId={EventId} PaymentId={PaymentId} Attempt={Attempt} Error={Error}",
                        delivery.Id, delivery.PaymentId, delivery.AttemptCount, error);
            }

            // Gönderilen sonuç, kapanma sinyali gelse bile kaydedilir (tekrar gönderimi azaltmak için).
            await db.SaveChangesAsync(CancellationToken.None);
        }

        await tx.CommitAsync(CancellationToken.None);
        return processed;
    }
}
