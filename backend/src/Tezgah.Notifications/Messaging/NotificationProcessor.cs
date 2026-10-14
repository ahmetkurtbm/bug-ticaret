using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tezgah.Contracts.Events;
using Tezgah.Notifications.Domain;
using Tezgah.Notifications.Email;
using Tezgah.Notifications.Logic;
using Tezgah.Notifications.Persistence;

namespace Tezgah.Notifications.Messaging;

public enum ProcessResult
{
    /// <summary>İşlendi (gönderildi, gönderim hatası kayda geçti ya da zaten işlenmişti): ack.</summary>
    Done,
    /// <summary>Bilinmeyen tip ya da bozuk gövde: nack, requeue yok (DLQ).</summary>
    Invalid,
}

/// <summary>Tek bir olay mesajını işler: tekilleştirir, kaydeder, e-postayı gönderir.</summary>
public class NotificationProcessor(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<NotificationProcessor> logger)
{
    public async Task<ProcessResult> ProcessAsync(string? eventType, ReadOnlyMemory<byte> body, CancellationToken ct)
    {
        if (eventType is null || !TryBuild(eventType, body, out var eventId, out var recipient, out var content))
            return ProcessResult.Invalid;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var id = Guid.NewGuid();
        var now = timeProvider.GetUtcNow();

        // Çağıran, ProcessResult.Done dönünce ack'ler; yani ack satır veritabanına yazıldıktan sonra verilir.
        await using (var tx = await db.Database.BeginTransactionAsync(ct))
        {
            var inserted = await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO notifications (id, event_id, event_type, recipient_email, subject, body_html, status, attempts, created_at)
                VALUES ({id}, {eventId}, {eventType}, {recipient}, {content.Subject}, {content.BodyHtml}, {NotificationStatus.Pending}, 0, {now})
                ON CONFLICT (event_id) DO NOTHING
                """, ct);
            await tx.CommitAsync(ct);

            if (inserted == 0)
            {
                logger.LogInformation("Olay zaten işlenmiş, atlandı: {EventId}", eventId);
                return ProcessResult.Done;
            }
        }

        var notification = await db.Notifications.SingleAsync(n => n.Id == id, ct);
        await DeliverAsync(db, scope.ServiceProvider.GetRequiredService<IEmailSender>(), notification, ct);
        return ProcessResult.Done;
    }

    /// <summary>E-postayı gönderir; sonucu (sent / failed, attempts, last_error) satıra yazar.</summary>
    public async Task DeliverAsync(NotificationsDbContext db, IEmailSender sender, Notification n, CancellationToken ct)
    {
        n.Attempts++;
        try
        {
            await sender.SendAsync(n.RecipientEmail, n.Subject, n.BodyHtml, ct);
            n.Status = NotificationStatus.Sent;
            n.SentAt = timeProvider.GetUtcNow();
            n.LastError = null;
            logger.LogInformation("E-posta gönderildi: {EventType} {EventId}", n.EventType, n.EventId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            n.Status = NotificationStatus.Failed;
            var error = $"{ex.GetType().Name}: {ex.Message}";
            n.LastError = error.Length > 1000 ? error[..1000] : error;
            logger.LogWarning(ex, "E-posta gönderilemedi ({Attempts}. deneme): {EventId}", n.Attempts, n.EventId);
        }
        // Gönderim sonucu iptal edilse bile kaydedilir; yoksa gönderilmiş e-posta tekrar gönderilebilir.
        await db.SaveChangesAsync(CancellationToken.None);
    }

    private bool TryBuild(string eventType, ReadOnlyMemory<byte> body, out Guid eventId, out string recipient, out EmailContent content)
    {
        eventId = Guid.Empty;
        recipient = "";
        content = null!;

        var clrType = MessageTypeResolver.Resolve(eventType);
        if (clrType is null)
        {
            logger.LogWarning("Bilinmeyen olay tipi, DLQ'ya gidiyor: {EventType}", eventType);
            return false;
        }

        object? evt;
        try
        {
            evt = JsonSerializer.Deserialize(body.Span, clrType, JsonSerializerOptions.Web);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Bozuk JSON, DLQ'ya gidiyor: {EventType}", eventType);
            return false;
        }

        switch (evt)
        {
            case OrderPaidEvent e when e.Lines is not null:
                (eventId, recipient, content) = (e.EventId, e.CustomerEmail, EmailTemplates.OrderPaid(e));
                break;
            case OrderShippedEvent e:
                (eventId, recipient, content) = (e.EventId, e.CustomerEmail, EmailTemplates.OrderShipped(e));
                break;
            case SellerApprovedEvent e:
                (eventId, recipient, content) = (e.EventId, e.OwnerEmail, EmailTemplates.SellerApproved(e));
                break;
            default:
                logger.LogWarning("Olay gövdesi eksik, DLQ'ya gidiyor: {EventType}", eventType);
                return false;
        }

        if (eventId == Guid.Empty || string.IsNullOrWhiteSpace(recipient))
        {
            logger.LogWarning("EventId ya da alıcı e-postası boş, DLQ'ya gidiyor: {EventType}", eventType);
            return false;
        }
        return true;
    }
}
