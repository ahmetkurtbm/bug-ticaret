using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tezgah.Application.Common.Interfaces;
using Tezgah.Domain.Orders;

namespace Tezgah.Application.Orders;

/// <summary>Süresi içinde ödenmeyen siparişleri iptal eder ve ayrılmış stoğu geri bırakır.</summary>
public class ExpireUnpaidOrders(ITezgahDbContext db, IOptions<OrderOptions> options, TimeProvider clock, ILogger<ExpireUnpaidOrders> logger)
{
    public async Task<int> RunAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var cutoff = now.AddMinutes(-options.Value.PaymentTimeoutMinutes);
        var ids = await db.Orders.AsNoTracking()
            .Where(o => o.Status == OrderStatus.PendingPayment && o.CreatedAt < cutoff)
            .OrderBy(o => o.CreatedAt)
            .Select(o => o.Id)
            .Take(100)
            .ToListAsync(ct);

        var expired = 0;
        foreach (var id in ids)
        {
            try
            {
                if (await ExpireAsync(id, now, ct)) expired++;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                // Tek bir siparişteki hata sıradakilerin iptalini durdurmasın.
                logger.LogError(ex, "Sipariş iptal edilemedi {OrderId}", id);
                db.ChangeTracker.Clear();
            }
        }

        if (expired > 0) logger.LogInformation("{Count} ödenmemiş sipariş iptal edildi", expired);
        return expired;
    }

    private async Task<bool> ExpireAsync(long id, DateTimeOffset now, CancellationToken ct)
    {
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            // Koşullu güncelleme: bu arada ödeme geldiyse sipariş artık PendingPayment değildir, dokunulmaz.
            var claimed = await db.Orders
                .Where(o => o.Id == id && o.Status == OrderStatus.PendingPayment)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.Status, OrderStatus.Cancelled)
                    .SetProperty(o => o.CancelledAt, now)
                    .SetProperty(o => o.UpdatedAt, now), ct);
            if (claimed == 0) return false;

            var order = await db.Orders.AsNoTracking().Include(o => o.Items).SingleAsync(o => o.Id == id, ct);
            await OrderStockTransitions.ReleaseAsync(db, order, now, ct);

            // Ödenmeyen siparişin kupon kullanımı iade edilir; yoksa limit ödenmemiş siparişlerle tükenir.
            var usages = await db.CouponUsages.Where(u => u.OrderId == id).ToListAsync(ct);
            foreach (var usage in usages)
            {
                await db.Coupons
                    .Where(c => c.Id == usage.CouponId && c.UsedCount > 0)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedCount, c => c.UsedCount - 1), ct);
            }
            db.CouponUsages.RemoveRange(usages);

            db.OrderStatusHistory.Add(new OrderStatusHistory
            {
                OrderId = id,
                FromStatus = OrderStatus.PendingPayment,
                ToStatus = OrderStatus.Cancelled,
                Note = "Ödeme süresi doldu",
                ChangedAt = now,
            });
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return true;
        }
    }
}
