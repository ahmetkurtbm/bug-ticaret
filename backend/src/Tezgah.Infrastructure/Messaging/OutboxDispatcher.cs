using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tezgah.Domain.Messaging;
using Tezgah.Infrastructure.Persistence;

namespace Tezgah.Infrastructure.Messaging;

/// <summary>
/// Outbox'taki bekleyen olayları yayınlar. Satırlar FOR UPDATE SKIP LOCKED ile alınır: API birden fazla
/// örnekle çalışsa da aynı olay iki örnek tarafından aynı anda gönderilmez. Yine de en az bir kez teslim
/// garantisi vardır (yayınlandı ama işaretlenemeden çökerse tekrar gider); tüketici EventId ile tekilleştirir.
/// </summary>
public class OutboxDispatcher(TezgahDbContext db, RabbitMqPublisher publisher, TimeProvider clock, ILogger<OutboxDispatcher> logger)
{
    private const int BatchSize = 50;

    public async Task<int> DispatchAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var batch = await db.OutboxMessages
            .FromSql($"SELECT * FROM outbox_messages WHERE published_at IS NULL AND next_attempt_at <= {now} ORDER BY occurred_at LIMIT {BatchSize} FOR UPDATE SKIP LOCKED")
            .ToListAsync(ct);
        if (batch.Count == 0) return 0;

        var sent = 0;
        foreach (var message in batch)
        {
            try
            {
                await publisher.PublishAsync(message.Id, message.Type, Encoding.UTF8.GetBytes(message.Payload), message.OccurredAt, ct);
                message.PublishedAt = clock.GetUtcNow();
                message.LastError = null;
                sent++;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                message.Attempts++;
                message.NextAttemptAt = clock.GetUtcNow() + Backoff(message.Attempts);
                message.LastError = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
                logger.LogWarning(ex, "Olay yayınlanamadı {MessageId} {Type} deneme {Attempts}", message.Id, message.Type, message.Attempts);
                await publisher.ResetAsync();
                // Bağlantı koptuysa partinin geri kalanı da başarısız olur; sonraki tura bırak.
                break;
            }
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return sent;
    }

    public static TimeSpan Backoff(int attempts) => TimeSpan.FromSeconds(Math.Min(300, Math.Pow(2, Math.Min(attempts, 9))));
}
