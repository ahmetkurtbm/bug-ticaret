using Tezgah.Application.Pricing;
using Tezgah.DataGen.Data;
using Tezgah.Domain.Orders;
using Tezgah.Domain.Payments;
using Tezgah.Domain.Shipping;

namespace Tezgah.DataGen;

public partial class DataGenerator
{
    private async Task WriteOrdersAsync()
    {
        await using var orders = await BulkWriter.OpenAsync(dataSource, "orders",
            "id", "order_number", "user_id", "status", "subtotal", "discount_total", "shipping_total", "vat_total", "grand_total",
            "currency", "coupon_code",
            "shipping_full_name", "shipping_phone", "shipping_city", "shipping_district", "shipping_line", "shipping_postal_code",
            "billing_full_name", "billing_phone", "billing_city", "billing_district", "billing_line", "billing_postal_code",
            "placed_at", "paid_at", "cancelled_at", "created_at");
        await using var items = await BulkWriter.OpenAsync(dataSource, "order_items",
            "id", "order_id", "seller_id", "variant_id", "product_name", "sku", "variant_label", "quantity", "unit_price", "vat_rate",
            "discount_amount", "line_total", "commission_rate");
        await using var history = await BulkWriter.OpenAsync(dataSource, "order_status_history",
            "id", "order_id", "from_status", "to_status", "note", "changed_by_user_id", "changed_at");
        await using var payments = await BulkWriter.OpenAsync(dataSource, "payments",
            "id", "order_id", "provider", "provider_payment_id", "amount", "currency", "status", "installment_count", "failure_reason", "completed_at", "created_at");
        await using var usages = await BulkWriter.OpenAsync(dataSource, "coupon_usages", "id", "coupon_id", "user_id", "order_id", "discount_amount", "used_at");
        await using var shipments = await BulkWriter.OpenAsync(dataSource, "shipments",
            "id", "order_id", "seller_id", "carrier", "tracking_number", "status", "shipped_at", "delivered_at", "created_at");
        await using var events = await BulkWriter.OpenAsync(dataSource, "shipment_events", "id", "shipment_id", "status", "location", "description", "occurred_at");
        await using var reviews = await BulkWriter.OpenAsync(dataSource, "reviews",
            "id", "product_id", "user_id", "order_item_id", "rating", "comment", "is_approved", "created_at");

        long itemId = 1, historyId = 1, paymentId = 1, usageId = 1, shipmentId = 1, eventId = 1, reviewId = 1;
        var commission = _sellers.ToDictionary(s => s.Id, s => s.CommissionRate);
        var couponUsedCount = new Dictionary<long, int>();
        var perCouponUser = new HashSet<(long, long)>();

        for (long orderId = 1; orderId <= _p.Orders; orderId++)
        {
            // Siparişlerin bir kısmı aynı sadık müşterilerden gelsin
            var customer = Chance(0.3) ? _customers[_rng.Next(Math.Max(1, _customers.Count / 10))] : Pick(_customers);
            var ageDays = (int)Math.Floor(Math.Pow(_rng.NextDouble(), 1.6) * 730);
            var placedAt = _now.AddDays(-ageDays).AddSeconds(-_rng.Next(0, 86_400));

            // Satırlar
            var lineCount = _rng.NextDouble() switch { < 0.55 => 1, < 0.8 => 2, < 0.93 => 3, _ => 4 };
            var lines = new List<(VariantInfo V, int Qty, decimal UnitPrice)>();
            for (var i = 0; i < lineCount; i++)
            {
                var v = Chance(0.7) ? _variants[(int)_popularVariantIndexes[_rng.Next(_popularVariantIndexes.Length)]] : Pick(_variants);
                if (lines.Any(l => l.V.Id == v.Id)) continue;
                // Geçmiş siparişler o günkü fiyattan: bugünkü fiyata göre biraz oynat
                var unit = ageDays > 30 ? RoundPrice(v.Price * (decimal)(0.85 + _rng.NextDouble() * 0.25)) : v.Price;
                lines.Add((v, _rng.NextDouble() < 0.85 ? 1 : _rng.Next(2, 4), unit));
            }

            // Kupon
            CouponInfo? coupon = null;
            if (Chance(0.12))
            {
                var candidates = _coupons.Where(c => c.StartsAt <= placedAt && c.EndsAt > placedAt && c.Code != "ESKIKOD").ToList();
                if (candidates.Count > 0) coupon = Pick(candidates);
            }
            var pricing = CartPricingCalculator.Calculate(
                lines.Select(l => new PricingLine(l.V.Id, l.V.SellerId, l.UnitPrice, l.Qty, l.V.VatRate)).ToList(),
                coupon is null ? null : new CouponRule(coupon.Code, coupon.Type, coupon.Value, coupon.MaxDiscount, coupon.MinOrder, coupon.SellerId));
            if (pricing.CouponError is not null) coupon = null;
            if (coupon is not null && pricing.CouponError is null)
            {
                couponUsedCount[coupon.Id] = couponUsedCount.GetValueOrDefault(coupon.Id) + 1;
            }

            // Durum: eski siparişler büyük ölçüde teslim edilmiş
            var status = PickStatus(ageDays);
            var paidAt = status is OrderStatus.PendingPayment or OrderStatus.Cancelled && Chance(0.5) ? (DateTime?)null : placedAt.AddMinutes(_rng.Next(1, 30));
            if (status == OrderStatus.PendingPayment) paidAt = null;
            var cancelledAt = status == OrderStatus.Cancelled ? placedAt.AddHours(_rng.Next(1, 72)) : (DateTime?)null;

            // Eski sistemden taşınan siparişlerin bir kısmında adres yok (bilerek kirli; NET-03)
            var legacyNoAddress = ageDays > 540 && Chance(0.04);
            var addr = legacyNoAddress ? null : customer;
            var billingSame = !Chance(0.1);

            await orders.RowAsync(orderId, $"TZ-{placedAt.Year}-{orderId:D7}", customer.Id, (int)status,
                pricing.Subtotal, pricing.DiscountTotal, pricing.ShippingTotal, pricing.VatTotal, pricing.GrandTotal, "TRY",
                coupon?.Code,
                addr?.FullName, addr?.Phone, addr?.City, addr?.District, addr?.Line, addr?.PostalCode,
                addr is null ? null : addr.FullName, addr is null ? null : addr.Phone, addr?.City,
                addr is null ? null : (billingSame ? addr.District : Pick(TurkishData.Districts[addr.City])),
                addr is null ? null : (billingSame ? addr.Line : $"{Pick(TurkishData.Streets)} No:{_rng.Next(1, 90)}"),
                addr?.PostalCode,
                placedAt, paidAt, cancelledAt, placedAt);

            var orderItemIds = new List<(long ItemId, VariantInfo V)>();
            foreach (var l in lines)
            {
                var discount = pricing.LineDiscounts[l.V.Id];
                var id = itemId++;
                await items.RowAsync(id, orderId, l.V.SellerId, l.V.Id, l.V.ProductName, l.V.Sku, l.V.Label, l.Qty, l.UnitPrice,
                    l.V.VatRate, discount, l.UnitPrice * l.Qty - discount, commission.GetValueOrDefault(l.V.SellerId, 12.5m));
                orderItemIds.Add((id, l.V));
            }

            if (coupon is not null)
                await usages.RowAsync(usageId++, coupon.Id, customer.Id, orderId, pricing.DiscountTotal, placedAt);

            // Durum geçmişi
            var timeline = BuildTimeline(status, placedAt);
            OrderStatus? previous = null;
            foreach (var (to, at) in timeline)
            {
                await history.RowAsync(historyId++, orderId, previous is null ? null : (int)previous.Value, (int)to, null, null, at);
                previous = to;
            }

            // Ödeme
            if (paidAt is not null || status == OrderStatus.Cancelled)
            {
                var payStatus = paidAt is null ? PaymentStatus.Failed
                    : status == OrderStatus.Returned ? PaymentStatus.Refunded
                    : PaymentStatus.Succeeded;
                await payments.RowAsync(paymentId, orderId, "sanal-pos", $"SP{orderId:D9}{_rng.Next(100, 999)}", pricing.GrandTotal, "TRY",
                    (int)payStatus, pricing.GrandTotal > 1000 && Chance(0.3) ? Pick(new[] { 2, 3, 6, 9 }) : 1,
                    payStatus == PaymentStatus.Failed ? Pick(new[] { "Yetersiz bakiye", "3D doğrulama başarısız", "Kart limiti aşıldı" }) : null,
                    paidAt ?? cancelledAt, placedAt);
                paymentId++;
            }

            // Kargo: her satıcı için ayrı gönderi
            if (status is OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Returned)
            {
                foreach (var sellerId in lines.Select(l => l.V.SellerId).Distinct())
                {
                    var shippedAt = placedAt.AddHours(_rng.Next(6, 72));
                    var delivered = status != OrderStatus.Shipped;
                    var deliveredAt = delivered ? shippedAt.AddHours(_rng.Next(18, 120)) : (DateTime?)null;
                    var sid = shipmentId++;
                    var carrier = Pick(TurkishData.Carriers);
                    await shipments.RowAsync(sid, orderId, sellerId, carrier, $"{carrier[..2].ToUpperInvariant()}{_rng.NextInt64(100_000_000, 999_999_999)}",
                        (int)(delivered ? ShipmentStatus.Delivered : ShipmentStatus.InTransit), shippedAt, deliveredAt, shippedAt);
                    await events.RowAsync(eventId++, sid, (int)ShipmentStatus.PickedUp, "Çıkış şubesi", "Gönderi kargoya verildi", shippedAt);
                    await events.RowAsync(eventId++, sid, (int)ShipmentStatus.InTransit, "Transfer merkezi", "Transfer merkezinde", shippedAt.AddHours(8));
                    if (delivered)
                    {
                        await events.RowAsync(eventId++, sid, (int)ShipmentStatus.OutForDelivery, addr?.District ?? "Teslimat şubesi", "Dağıtıma çıktı", deliveredAt!.Value.AddHours(-4));
                        await events.RowAsync(eventId++, sid, (int)ShipmentStatus.Delivered, addr?.District ?? "Teslimat şubesi", "Teslim edildi", deliveredAt.Value);
                    }
                }
            }

            // Yorumlar
            if (status == OrderStatus.Delivered)
            {
                foreach (var (oiId, v) in orderItemIds)
                {
                    if (!Chance(0.3)) continue;
                    var rating = _rng.NextDouble() switch { < 0.55 => 5, < 0.8 => 4, < 0.9 => 3, < 0.95 => 2, _ => 1 };
                    var comment = rating >= 4 ? Pick(TurkishData.PositiveComments) : rating == 3 ? Pick(TurkishData.NeutralComments) : Pick(TurkishData.NegativeComments);
                    await reviews.RowAsync(reviewId++, v.ProductId, customer.Id, oiId, rating, Chance(0.8) ? comment : null, !Chance(0.05),
                        placedAt.AddDays(_rng.Next(3, 20)));
                }
            }
        }

        await orders.DisposeAsync();
        await usages.DisposeAsync();
        foreach (var (couponId, used) in couponUsedCount)
            await ExecAsync($"UPDATE coupons SET used_count = {used} WHERE id = {couponId}");
    }

    private OrderStatus PickStatus(int ageDays)
    {
        var r = _rng.NextDouble();
        if (ageDays < 2) return r switch { < 0.15 => OrderStatus.PendingPayment, < 0.55 => OrderStatus.Paid, < 0.9 => OrderStatus.Preparing, _ => OrderStatus.Cancelled };
        if (ageDays < 7) return r switch { < 0.03 => OrderStatus.PendingPayment, < 0.15 => OrderStatus.Preparing, < 0.75 => OrderStatus.Shipped, < 0.92 => OrderStatus.Delivered, _ => OrderStatus.Cancelled };
        return r switch { < 0.84 => OrderStatus.Delivered, < 0.93 => OrderStatus.Cancelled, _ => OrderStatus.Returned };
    }

    private List<(OrderStatus Status, DateTime At)> BuildTimeline(OrderStatus final, DateTime placedAt)
    {
        var list = new List<(OrderStatus, DateTime)> { (OrderStatus.PendingPayment, placedAt) };
        if (final == OrderStatus.PendingPayment) return list;
        if (final == OrderStatus.Cancelled)
        {
            list.Add((OrderStatus.Cancelled, placedAt.AddHours(_rng.Next(1, 72))));
            return list;
        }
        var t = placedAt.AddMinutes(_rng.Next(1, 30));
        list.Add((OrderStatus.Paid, t));
        if (final == OrderStatus.Paid) return list;
        list.Add((OrderStatus.Preparing, t = t.AddHours(_rng.Next(1, 24))));
        if (final == OrderStatus.Preparing) return list;
        list.Add((OrderStatus.Shipped, t = t.AddHours(_rng.Next(4, 48))));
        if (final == OrderStatus.Shipped) return list;
        list.Add((OrderStatus.Delivered, t = t.AddHours(_rng.Next(18, 120))));
        if (final == OrderStatus.Delivered) return list;
        list.Add((OrderStatus.Returned, t.AddDays(_rng.Next(2, 14))));
        return list;
    }

    private async Task UpdateAggregatesAsync()
    {
        await ExecAsync("""
            UPDATE products p SET sold_count = s.sold
            FROM (SELECT v.product_id, SUM(oi.quantity) AS sold
                  FROM order_items oi JOIN product_variants v ON v.id = oi.variant_id
                  JOIN orders o ON o.id = oi.order_id
                  WHERE o.status IN (2, 3, 4, 5)
                  GROUP BY v.product_id) s
            WHERE s.product_id = p.id
            """);
        await ExecAsync("""
            UPDATE products p SET rating = r.avg_rating, review_count = r.cnt
            FROM (SELECT product_id, ROUND(AVG(rating)::numeric, 2) AS avg_rating, COUNT(*) AS cnt
                  FROM reviews WHERE is_approved GROUP BY product_id) r
            WHERE r.product_id = p.id
            """);
    }
}
