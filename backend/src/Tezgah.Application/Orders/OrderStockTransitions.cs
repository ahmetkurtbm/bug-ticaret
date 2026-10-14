using Microsoft.EntityFrameworkCore;
using Tezgah.Application.Common.Interfaces;
using Tezgah.Domain.Inventory;
using Tezgah.Domain.Orders;

namespace Tezgah.Application.Orders;

/// <summary>Ayrılmış stoğun satışa dönüşmesi ya da geri bırakılması. Çağıran bir işlem (transaction) içinde olmalı.</summary>
public static class OrderStockTransitions
{
    public static async Task CommitSaleAsync(ITezgahDbContext db, Order order, DateTimeOffset now, CancellationToken ct)
    {
        foreach (var item in order.Items.Where(i => i.StockItemId is not null))
        {
            await db.StockItems
                .Where(s => s.Id == item.StockItemId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Quantity, x => x.Quantity - item.Quantity)
                    .SetProperty(x => x.Reserved, x => x.Reserved - item.Quantity)
                    .SetProperty(x => x.UpdatedAt, now), ct);
            db.StockMovements.Add(new StockMovement
            {
                StockItemId = item.StockItemId!.Value,
                Delta = -item.Quantity,
                Reason = StockMovementReason.Sale,
                ReferenceType = "order",
                ReferenceId = order.Id,
                CreatedAt = now,
            });
        }
    }

    public static async Task ReleaseAsync(ITezgahDbContext db, Order order, DateTimeOffset now, CancellationToken ct)
    {
        foreach (var item in order.Items.Where(i => i.StockItemId is not null))
        {
            await db.StockItems
                .Where(s => s.Id == item.StockItemId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Reserved, x => x.Reserved - item.Quantity)
                    .SetProperty(x => x.UpdatedAt, now), ct);
        }
    }
}
