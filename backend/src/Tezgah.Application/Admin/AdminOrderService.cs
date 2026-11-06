using Microsoft.EntityFrameworkCore;
using Tezgah.Application.Common.Exceptions;
using Tezgah.Application.Common.Interfaces;
using Tezgah.Application.Common.Models;

namespace Tezgah.Application.Admin;

public interface IAdminOrderService
{
    Task<PagedResult<AdminOrderListItemDto>> ListAsync(AdminOrderQuery query, CancellationToken ct);
    Task<AdminOrderDetailDto> GetAsync(string orderNumber, CancellationToken ct);
}

public class AdminOrderService(ITezgahDbContext db) : IAdminOrderService
{
    private static readonly TimeZoneInfo Istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public async Task<PagedResult<AdminOrderListItemDto>> ListAsync(AdminOrderQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, 100);
        var query = db.Orders.AsNoTracking();

        if (q.Status is not null) query = query.Where(o => o.Status == q.Status);
        if (q.From is { } from) { var f = StartOfDay(from); query = query.Where(o => o.PlacedAt >= f); }
        if (q.To is { } to) { var t = StartOfDay(to.AddDays(1)); query = query.Where(o => o.PlacedAt < t); }
        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            var term = q.Q.Trim();
            query = term.StartsWith("TZ-", StringComparison.OrdinalIgnoreCase)
                ? query.Where(o => o.OrderNumber == term.ToUpperInvariant())
                : query.Where(o => o.User.NormalizedEmail == Common.Text.SlugHelper.NormalizeEmail(term));
        }

        var total = await query.CountAsync(ct);
        var orders = await query
            .OrderByDescending(o => o.PlacedAt).ThenByDescending(o => o.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        var items = new List<AdminOrderListItemDto>(orders.Count);
        foreach (var o in orders)
        {
            var customer = await db.Users.AsNoTracking().SingleAsync(u => u.Id == o.UserId, ct);
            var lines = await db.OrderItems.AsNoTracking().Where(i => i.OrderId == o.Id).ToListAsync(ct);
            items.Add(new AdminOrderListItemDto(
                o.Id, o.OrderNumber, customer.Email, $"{customer.FirstName} {customer.LastName}", o.PlacedAt, o.Status, o.GrandTotal,
                lines.Sum(i => i.Quantity), lines.Select(i => i.SellerId).Distinct().Count(), o.CouponCode));
        }
        return new PagedResult<AdminOrderListItemDto>(items, page, pageSize, total);
    }

    public async Task<AdminOrderDetailDto> GetAsync(string orderNumber, CancellationToken ct)
    {
        var o = await db.Orders.AsNoTracking().AsSplitQuery()
            .Where(x => x.OrderNumber == orderNumber)
            .Select(x => new
            {
                x.Id, x.OrderNumber, x.User.Email, Name = x.User.FirstName + " " + x.User.LastName, x.Status, x.PlacedAt, x.PaidAt,
                x.Subtotal, x.DiscountTotal, x.ShippingTotal, x.GrandTotal, x.CouponCode,
                City = x.ShippingAddress == null ? null : x.ShippingAddress.City,
                Items = x.Items.OrderBy(i => i.Id).Select(i => new AdminOrderItemDto(i.ProductName, i.Sku, i.VariantLabel,
                    db.Sellers.Where(s => s.Id == i.SellerId).Select(s => s.StoreName).FirstOrDefault() ?? "", i.Quantity, i.UnitPrice,
                    i.DiscountAmount, i.LineTotal, i.CommissionRate)).ToList(),
                History = x.StatusHistory.OrderBy(h => h.ChangedAt).Select(h => new AdminStatusEntryDto(h.FromStatus, h.ToStatus, h.Note, h.ChangedAt)).ToList(),
            })
            .SingleOrDefaultAsync(ct) ?? throw new NotFoundException("Sipariş bulunamadı.");

        var payments = await db.Payments.AsNoTracking().Where(p => p.OrderId == o.Id).OrderBy(p => p.CreatedAt)
            .Select(p => new AdminPaymentDto(p.Provider, p.ProviderPaymentId, p.Amount, p.InstallmentCount, p.Status, p.FailureReason, p.CreatedAt, p.CompletedAt))
            .ToListAsync(ct);
        var shipments = await db.Shipments.AsNoTracking().Where(s => s.OrderId == o.Id)
            .Select(s => new AdminShipmentDto(db.Sellers.Where(x => x.Id == s.SellerId).Select(x => x.StoreName).FirstOrDefault() ?? "", s.Carrier, s.TrackingNumber, s.Status, s.ShippedAt))
            .ToListAsync(ct);

        return new AdminOrderDetailDto(o.Id, o.OrderNumber, o.Email, o.Name, o.Status, o.PlacedAt, o.PaidAt, o.Subtotal, o.DiscountTotal,
            o.ShippingTotal, o.GrandTotal, o.CouponCode, o.City, o.Items, payments, shipments, o.History);
    }

    private static DateTimeOffset StartOfDay(DateOnly day)
    {
        var local = day.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(local, Istanbul.GetUtcOffset(local)).ToUniversalTime();
    }
}
