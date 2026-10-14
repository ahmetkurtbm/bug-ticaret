using Npgsql;

namespace Tezgah.DataGen;

public static class Database
{
    private static readonly string[] Tables =
    [
        "audit_logs", "reviews", "shipment_events", "shipments", "payments", "coupon_usages",
        "order_status_history", "order_items", "orders", "cart_items", "carts", "coupons",
        "stock_movements", "stock_items", "warehouses", "price_history", "product_attribute_values",
        "product_attributes", "product_images", "product_variants", "products", "brands",
        "category_commissions", "categories", "sellers", "refresh_tokens", "addresses",
        "user_roles", "roles", "users",
    ];

    public static async Task<bool> HasDataAsync(NpgsqlConnection conn)
    {
        await using var cmd = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM users)", conn);
        return (bool)(await cmd.ExecuteScalarAsync())!;
    }

    public static async Task TruncateAllAsync(NpgsqlConnection conn)
    {
        Console.WriteLine("Tablolar boşaltılıyor…");
        await using var cmd = new NpgsqlCommand($"TRUNCATE {string.Join(", ", Tables)} RESTART IDENTITY CASCADE", conn);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>COPY ile kimlikler elle verildiği için identity sayaçları en büyük değere çekilir.</summary>
    public static async Task ResetSequencesAsync(NpgsqlConnection conn)
    {
        foreach (var table in Tables.Where(t => t != "user_roles"))
        {
            await using var cmd = new NpgsqlCommand(
                $"SELECT setval(pg_get_serial_sequence('{table}', 'id'), COALESCE((SELECT MAX(id) FROM {table}), 0) + 1, false)", conn);
            await cmd.ExecuteNonQueryAsync();
        }
        await using var analyze = new NpgsqlCommand("ANALYZE", conn);
        await analyze.ExecuteNonQueryAsync();
    }
}
