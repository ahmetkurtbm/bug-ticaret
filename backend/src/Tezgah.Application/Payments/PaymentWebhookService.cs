using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tezgah.Application.Common.Interfaces;
using Tezgah.Application.Common.Messaging;
using Tezgah.Application.Orders;
using Tezgah.Contracts.Events;
using Tezgah.Contracts.Payments;
using Tezgah.Domain.Audit;
using Tezgah.Domain.Orders;
using Tezgah.Domain.Payments;

namespace Tezgah.Application.Payments;

public enum WebhookResult { Processed, Duplicate, Ignored }

public interface IPaymentWebhookService
{
    Task<WebhookResult> HandleAsync(PaymentWebhookEvent evt, CancellationToken ct);
}

/// <summary>
/// Ödeme servisinden gelen olayları işler. Servis olayları en az bir kez gönderir; aynı EventId
/// ikinci kez gelirse <see cref="ProcessedWebhookEvent"/> kaydı sayesinde hiçbir şey yapılmaz.
/// </summary>
public class PaymentWebhookService(ITezgahDbContext db, TimeProvider clock, ILogger<PaymentWebhookService> logger) : IPaymentWebhookService
{
    public async Task<WebhookResult> HandleAsync(PaymentWebhookEvent evt, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Önce olay kimliğini yaz: aynı anda gelen iki kopyadan sadece biri bu satırı ekleyebilir.
        var inserted = await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO processed_webhook_events (event_id, source, processed_at) VALUES ({evt.EventId}, 'payments', {now}) ON CONFLICT (event_id) DO NOTHING", ct);
        if (inserted == 0)
        {
            logger.LogInformation("Tekrarlanan ödeme olayı yok sayıldı {EventId}", evt.EventId);
            return WebhookResult.Duplicate;
        }

        var order = await db.Orders.Include(o => o.Items).SingleOrDefaultAsync(o => o.Id == evt.OrderId, ct);
        var providerId = evt.PaymentId.ToString();
        var payment = await db.Payments.SingleOrDefaultAsync(p => p.ProviderPaymentId == providerId, ct);

        if (order is null || order.OrderNumber != evt.OrderNumber)
        {
            logger.LogWarning("Ödeme olayı bilinmeyen siparişe ait {EventId} {OrderId}", evt.EventId, evt.OrderId);
            await tx.CommitAsync(ct);
            return WebhookResult.Ignored;
        }

        if (payment is null)
        {
            payment = new Payment { OrderId = order.Id, Provider = "tezgah-payments", ProviderPaymentId = providerId, Amount = evt.Amount, InstallmentCount = evt.InstallmentCount };
            db.Payments.Add(payment);
        }

        if (evt.Status == PaymentEventStatus.Succeeded)
        {
            payment.Status = PaymentStatus.Succeeded;
            payment.CompletedAt = evt.OccurredAt;

            // Koşullu geçiş: bu arada süre dolumu siparişi iptal ettiyse ya da başka bir ödeme
            // olayı onu zaten "ödendi" yaptıysa 0 satır etkilenir ve stok ikinci kez düşülmez.
            var claimed = await db.Orders
                .Where(o => o.Id == order.Id && o.Status == OrderStatus.PendingPayment && o.GrandTotal == evt.Amount)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.Status, OrderStatus.Paid)
                    .SetProperty(o => o.PaidAt, evt.OccurredAt)
                    .SetProperty(o => o.UpdatedAt, now), ct);
            if (claimed == 1)
            {
                db.OrderStatusHistory.Add(new OrderStatusHistory { OrderId = order.Id, FromStatus = OrderStatus.PendingPayment, ToStatus = OrderStatus.Paid, ChangedAt = now });
                await OrderStockTransitions.CommitSaleAsync(db, order, now, ct);

                var customer = await db.Users.AsNoTracking().Where(u => u.Id == order.UserId)
                    .Select(u => new { u.Email, Name = u.FirstName + " " + u.LastName }).SingleAsync(ct);
                var paidEvent = new OrderPaidEvent(Guid.CreateVersion7(), order.Id, order.OrderNumber, customer.Email, customer.Name, order.GrandTotal,
                    order.Items.OrderBy(i => i.Id).Select(i => new OrderLine(i.ProductName, i.VariantLabel, i.Quantity, i.LineTotal)).ToList(), now);
                Outbox.Add(db, paidEvent.EventId, EventTopology.OrderPaid, paidEvent, now);
            }
            else
            {
                // Süresi dolup iptal edilmiş siparişe geç gelen ödeme ya da tutar uyuşmazlığı: iade için işaretle.
                logger.LogError("Ödeme alındı ama sipariş onaylanamadı {OrderNumber} durum {Status} tutar {Amount}/{GrandTotal}",
                    order.OrderNumber, order.Status, evt.Amount, order.GrandTotal);
                db.AuditLogs.Add(new AuditLog
                {
                    Action = "payment.needs_refund",
                    EntityType = "order",
                    EntityId = order.Id,
                    Data = $"{{\"paymentId\":\"{evt.PaymentId}\",\"amount\":{evt.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}",
                    CreatedAt = now,
                });
            }
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = evt.FailureReason;
            payment.CompletedAt = evt.OccurredAt;
            // Sipariş ödeme bekliyor durumunda kalır; müşteri süre dolana kadar tekrar deneyebilir.
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        logger.LogInformation("Ödeme olayı işlendi {EventId} {OrderNumber} {Status}", evt.EventId, order.OrderNumber, evt.Status);
        return WebhookResult.Processed;
    }
}
