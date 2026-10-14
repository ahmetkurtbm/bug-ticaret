using Microsoft.EntityFrameworkCore;
using Tezgah.Notifications.Domain;
using Tezgah.Notifications.Email;
using Tezgah.Notifications.Persistence;

namespace Tezgah.Notifications.Messaging;

/// <summary>
/// 30 sn'de bir başarısız (attempts &lt; 5) e-postaları yeniden gönderir. Ayrıca satır yazılıp süreç gönderimden
/// önce çökmüşse kalan eski "pending" satırları da toplar.
/// </summary>
public class RetryWorker(
    IServiceScopeFactory scopeFactory,
    NotificationProcessor processor,
    TimeProvider timeProvider,
    ILogger<RetryWorker> logger) : BackgroundService
{
    public const int MaxAttempts = 5;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PendingGrace = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RetryBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Yeniden gönderim partisi başarısız.");
            }

            try
            {
                await Task.Delay(Interval, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RetryBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        var pendingCutoff = timeProvider.GetUtcNow() - PendingGrace;
        var batch = await db.Notifications
            .Where(n => n.Attempts < MaxAttempts &&
                        (n.Status == NotificationStatus.Failed ||
                         (n.Status == NotificationStatus.Pending && n.CreatedAt < pendingCutoff)))
            .OrderBy(n => n.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        foreach (var n in batch)
            await processor.DeliverAsync(db, sender, n, ct);

        if (batch.Count > 0)
            logger.LogInformation("{Count} e-posta yeniden denendi.", batch.Count);
    }
}
