using Tezgah.Domain.Campaigns;
using Tezgah.Domain.Inventory;

namespace Tezgah.DataGen;

public partial class DataGenerator
{
    private async Task WriteStockAsync()
    {
        await using var stock = await BulkWriter.OpenAsync(dataSource, "stock_items", "id", "variant_id", "warehouse_id", "quantity", "reserved", "created_at");
        await using var movements = await BulkWriter.OpenAsync(dataSource, "stock_movements",
            "id", "stock_item_id", "delta", "reason", "reference_type", "reference_id", "created_by_user_id", "created_at");
        await using var priceHistory = await BulkWriter.OpenAsync(dataSource, "price_history",
            "id", "variant_id", "old_price", "new_price", "changed_by_user_id", "changed_at");

        long nextMovementId = 1, nextPriceHistoryId = 1;
        var sellerOwners = _sellers.ToDictionary(s => s.Id, s => s.OwnerUserId);

        foreach (var v in _variants)
        {
            var quantity = Chance(0.1) ? 0 : (int)Math.Round(Math.Pow(_rng.NextDouble(), 2) * 200);
            var created = RandomPast(700, 30);
            await stock.RowAsync(v.Id, v.Id, v.SellerId, quantity, 0, created);
            await movements.RowAsync(nextMovementId++, v.Id, quantity, (int)StockMovementReason.InitialLoad, null, null,
                sellerOwners.GetValueOrDefault(v.SellerId), created);

            if (Chance(0.2))
            {
                var old = v.Price;
                var changes = _rng.Next(1, 4);
                for (var i = 0; i < changes; i++)
                {
                    var previous = RoundPrice(old * (decimal)(0.85 + _rng.NextDouble() * 0.4));
                    await priceHistory.RowAsync(nextPriceHistoryId++, v.Id, previous, old, sellerOwners.GetValueOrDefault(v.SellerId), RandomPast(400, 1));
                    old = previous;
                }
            }
        }
    }

    private async Task WriteCouponsAsync()
    {
        await using var coupons = await BulkWriter.OpenAsync(dataSource, "coupons",
            "id", "code", "description", "type", "value", "max_discount_amount", "min_order_amount", "max_uses",
            "max_uses_per_user", "used_count", "seller_id", "starts_at", "ends_at", "is_active", "created_at");

        var demoSeller = _sellers[0].Id;
        (string Code, string Desc, CouponType Type, decimal Value, decimal? Max, decimal Min, int? MaxUses, int? PerUser, long? Seller, int StartDays, int EndDays, bool Active)[] seed =
        [
            ("HOSGELDIN10", "İlk alışverişe %10 indirim", CouponType.Percentage, 10, 250, 200, null, 1, null, -400, 365, true),
            ("KARGOBEDAVA", "Ücretsiz kargo", CouponType.FreeShipping, 0, null, 150, null, null, null, -200, 120, true),
            ("SEPET100", "750 TL üzeri 100 TL indirim", CouponType.FixedAmount, 100, null, 750, 50_000, 3, null, -60, 45, true),
            ("KIS2026", "Kış sezonu %15", CouponType.Percentage, 15, 500, 500, 20_000, 1, null, -20, 70, true),
            ("YAZ2026", "Yaz indirimi (bitti)", CouponType.Percentage, 20, 400, 300, null, 1, null, -200, -60, true),
            ("BAHAR2027", "Bahar kampanyası (yakında)", CouponType.Percentage, 12, 300, 300, null, 1, null, 90, 150, true),
            ("DEMO50", "Demo Mağaza'da 50 TL", CouponType.FixedAmount, 50, null, 300, null, 2, demoSeller, -90, 90, true),
            ("FLAS25", "Flaş indirim %25", CouponType.Percentage, 25, 1000, 1000, 500, 1, null, -2, 1, true),
            ("ESKIKOD", "Kapatılmış kod", CouponType.Percentage, 30, null, 0, null, null, null, -500, 300, false),
        ];

        long id = 1;
        foreach (var c in seed)
        {
            var starts = _now.Date.AddDays(c.StartDays).AddHours(-3); // İstanbul gece yarısı = UTC 21:00
            var ends = _now.Date.AddDays(c.EndDays).AddHours(-3);
            await coupons.RowAsync(id, c.Code, c.Desc, (int)c.Type, c.Value, c.Max, c.Min, c.MaxUses, c.PerUser, 0, c.Seller, starts, ends, c.Active, starts.AddDays(-3));
            _coupons.Add(new CouponInfo(id, c.Code, c.Type, c.Value, c.Max, c.Min, c.Seller, starts, ends));
            id++;
        }
    }
}
