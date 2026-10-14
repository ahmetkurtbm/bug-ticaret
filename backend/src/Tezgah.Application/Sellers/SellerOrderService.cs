using Microsoft.EntityFrameworkCore;
using Tezgah.Application.Common.Exceptions;
using Tezgah.Application.Common.Interfaces;
using Tezgah.Application.Common.Messaging;
using Tezgah.Application.Common.Models;
using Tezgah.Contracts.Events;
using Tezgah.Domain.Catalog;
using Tezgah.Domain.Common;
using Tezgah.Domain.Orders;
using Tezgah.Domain.Shipping;

namespace Tezgah.Application.Sellers;

public interface ISellerOrderService
{
    Task<PagedResult<SellerOrderDto>> ListAsync(string filter, int page, int pageSize, CancellationToken ct);
    Task<SellerOrderDto> ShipAsync(string orderNumber, ShipOrderRequest request, CancellationToken ct);
    Task<SellerDashboardDto> GetDashboardAsync(CancellationToken ct);
}

public class SellerOrderService(ITezgahDbContext db, SellerContext seller, ICurrentUser currentUser, TimeProvider clock) : ISellerOrderService
{
    private static readonly TimeZoneInfo Istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public async Task<PagedResult<SellerOrderDto>> ListAsync(string filter, int page, int pageSize, CancellationToken ct)
    {
        var sellerId = await seller.GetActiveSellerIdAsync(ct);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = OrdersOf(sellerId);
        query = filter switch
        {
            SellerOrderFilter.ToShip => query.Where(o => (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Preparing)
                && !db.Shipments.Any(s => s.OrderId == o.Id && s.SellerId == sellerId)),
            SellerOrderFilter.Shipped => query.Where(o => db.Shipments.Any(s => s.OrderId == o.Id && s.SellerId == sellerId)),
            _ => query,
        };

        var total = await query.CountAsync(ct);
        // "Gönderilecekler" en eski siparişten başlar; diğer görünümler en yeniden.
        var ordered = filter == SellerOrderFilter.ToShip
            ? query.OrderBy(o => o.PaidAt).ThenBy(o => o.Id)
            : query.OrderByDescending(o => o.PlacedAt).ThenByDescending(o => o.Id);
        var items = await Project(ordered.Skip((page - 1) * pageSize).Take(pageSize), sellerId).ToListAsync(ct);
        return new PagedResult<SellerOrderDto>(items, page, pageSize, total);
    }

    public async Task<SellerOrderDto> ShipAsync(string orderNumber, ShipOrderRequest request, CancellationToken ct)
    {
        var sellerId = await seller.GetActiveSellerIdAsync(ct);
        var now = clock.GetUtcNow();
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var order = await db.Orders.Include(o => o.Items)
            .SingleOrDefaultAsync(o => o.OrderNumber == orderNumber && o.Items.Any(i => i.SellerId == sellerId), ct)
            ?? throw new NotFoundException("Sipariş bulunamadı.");
        if (order.Status is not (OrderStatus.Paid or OrderStatus.Preparing))
            throw new DomainException("order_not_shippable", "Yalnızca ödemesi alınmış ve kargoya verilmemiş siparişler gönderilebilir.");
        if (await db.Shipments.AnyAsync(s => s.OrderId == order.Id && s.SellerId == sellerId, ct))
            throw new ConflictException("already_shipped", "Bu siparişteki ürünleriniz zaten kargoya verildi.");

        var shipment = new Shipment
        {
            OrderId = order.Id,
            SellerId = sellerId,
            Carrier = request.Carrier,
            TrackingNumber = request.TrackingNumber,
            Status = ShipmentStatus.PickedUp,
            ShippedAt = now,
        };
        shipment.Events.Add(new ShipmentEvent { Status = ShipmentStatus.PickedUp, Description = "Kargo firmasına teslim edildi", OccurredAt = now });
        db.Shipments.Add(shipment);
        await db.SaveChangesAsync(ct);

        // Çok satıcılı siparişte her satıcı kendi paketini gönderir; hepsi gönderince sipariş "Kargoda" olur.
        var sellerIds = order.Items.Select(i => i.SellerId).Distinct().ToList();
        var shippedSellers = await db.Shipments.CountAsync(s => s.OrderId == order.Id, ct);
        var next = shippedSellers >= sellerIds.Count ? OrderStatus.Shipped : OrderStatus.Preparing;
        if (next != order.Status)
        {
            order.StatusHistory.Add(new OrderStatusHistory { OrderId = order.Id, FromStatus = order.Status, ToStatus = next, ChangedByUserId = currentUser.UserId, ChangedAt = now });
            order.Status = next;
        }

        var info = await db.Users.AsNoTracking().Where(u => u.Id == order.UserId)
            .Select(u => new { u.Email, Name = u.FirstName + " " + u.LastName, Store = db.Sellers.Where(s => s.Id == sellerId).Select(s => s.StoreName).Single() })
            .SingleAsync(ct);
        var shippedEvent = new OrderShippedEvent(Guid.CreateVersion7(), order.Id, order.OrderNumber, info.Email, info.Name, info.Store,
            request.Carrier, request.TrackingNumber, now);
        Outbox.Add(db, shippedEvent.EventId, EventTopology.OrderShipped, shippedEvent, now);

        // Satılan adet ürünün satış sayısına eklenir (vitrinde "çok satanlar" sıralaması).
        foreach (var group in order.Items.Where(i => i.SellerId == sellerId).GroupBy(i => i.VariantId))
        {
            var qty = group.Sum(i => i.Quantity);
            await db.Products.Where(p => p.Variants.Any(v => v.Id == group.Key))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.SoldCount, p => p.SoldCount + qty), ct);
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await Project(OrdersOf(sellerId).Where(o => o.Id == order.Id), sellerId).SingleAsync(ct);
    }

    public async Task<SellerDashboardDto> GetDashboardAsync(CancellationToken ct)
    {
        var sellerId = await seller.GetActiveSellerIdAsync(ct);
        var now = clock.GetUtcNow();
        var todayLocal = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, Istanbul).Date);
        var fromLocal = todayLocal.AddDays(-29);
        // Gün sınırları İstanbul saatine göre; veritabanına UTC karşılığı gönderilir.
        var from = new DateTimeOffset(fromLocal.ToDateTime(TimeOnly.MinValue), Istanbul.GetUtcOffset(fromLocal.ToDateTime(TimeOnly.MinValue))).ToUniversalTime();

        var toShip = await OrdersOf(sellerId).CountAsync(o => (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Preparing)
            && !db.Shipments.Any(s => s.OrderId == o.Id && s.SellerId == sellerId), ct);

        var lowStock = await db.ProductVariants.AsNoTracking()
            .Where(v => v.IsActive && v.Product.SellerId == sellerId && v.Product.Status == ProductStatus.Active)
            .CountAsync(v => (db.StockItems.Where(s => s.VariantId == v.Id).Sum(s => (int?)(s.Quantity - s.Reserved)) ?? 0) < SellerCatalogService.LowStockThreshold, ct);

        var productCounts = await db.Products.AsNoTracking().Where(p => p.SellerId == sellerId)
            .GroupBy(p => p.Status).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);

        var sales = await db.Orders.AsNoTracking()
            .Where(o => o.PaidAt >= from && o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Returned)
            .SelectMany(o => o.Items.Where(i => i.SellerId == sellerId)
                .Select(i => new { o.PaidAt, i.OrderId, i.LineTotal, i.CommissionRate }))
            .ToListAsync(ct);

        var daily = sales
            .GroupBy(s => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(s.PaidAt!.Value, Istanbul).Date))
            .ToDictionary(g => g.Key, g => new DailySalesPoint(g.Key, g.Sum(x => x.LineTotal), g.Select(x => x.OrderId).Distinct().Count()));
        var series = Enumerable.Range(0, 30).Select(d => fromLocal.AddDays(d))
            .Select(d => daily.TryGetValue(d, out var p) ? p : new DailySalesPoint(d, 0, 0)).ToList();

        var gross = sales.Sum(s => s.LineTotal);
        var commission = sales.Sum(s => Pricing.Money.Round(s.LineTotal * s.CommissionRate / 100m));
        return new SellerDashboardDto(
            toShip, lowStock,
            productCounts.FirstOrDefault(c => c.Key == ProductStatus.Active)?.Count ?? 0,
            productCounts.FirstOrDefault(c => c.Key == ProductStatus.PendingApproval)?.Count ?? 0,
            gross, commission, gross - commission, series);
    }

    private IQueryable<Order> OrdersOf(long sellerId) =>
        db.Orders.AsNoTracking().Where(o => o.Status != OrderStatus.PendingPayment && o.Items.Any(i => i.SellerId == sellerId));

    private IQueryable<SellerOrderDto> Project(IQueryable<Order> source, long sellerId) =>
        source.Select(o => new SellerOrderDto(
            o.OrderNumber,
            o.PlacedAt,
            o.Status,
            o.ShippingAddress == null ? null : o.ShippingAddress.FullName,
            o.ShippingAddress == null ? null : o.ShippingAddress.City,
            o.ShippingAddress == null ? null : o.ShippingAddress.District,
            o.ShippingAddress == null ? null : o.ShippingAddress.Line,
            o.Items.Where(i => i.SellerId == sellerId).OrderBy(i => i.Id)
                .Select(i => new SellerOrderItemDto(i.ProductName, i.Sku, i.VariantLabel, i.Quantity, i.UnitPrice, i.LineTotal)).ToList(),
            o.Items.Where(i => i.SellerId == sellerId).Sum(i => i.LineTotal),
            db.Shipments.Where(s => s.OrderId == o.Id && s.SellerId == sellerId)
                .Select(s => new SellerShipmentDto(s.Carrier, s.TrackingNumber, s.ShippedAt)).FirstOrDefault()));
}
