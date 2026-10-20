using Dapper;
using Npgsql;
using Tezgah.Application.Reporting;

namespace Tezgah.Infrastructure.Reporting;

/// <summary>
/// Eski raporlama modülü (legacy). Bağlantıyı kendisi açar, SQL'i elle yazar.
/// Tüm gün hesapları İstanbul saatine göre: "1 Kasım satışları" İstanbul'da 1 Kasım 00:00 ile 2 Kasım 00:00 arası.
/// </summary>
public class DapperReportingQueries(NpgsqlDataSource dataSource) : IReportingQueries
{
    private const string Tz = "Europe/Istanbul";
    // Ödemesi alınmış, iptal ya da iade edilmemiş siparişler: 2=Paid ... 5=Delivered.
    private const string SoldStatuses = "(2,3,4,5)";

    public async Task<SalesReport> GetSalesReportAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        var args = new { from = from.ToDateTime(TimeOnly.MinValue), toExclusive = to.AddDays(1).ToDateTime(TimeOnly.MinValue), tz = Tz };

        // Aralık sınırları yerel gün başlarıdır; (yerel zaman AT TIME ZONE tz) UTC anını verir, index kullanılabilir.
        const string range = "o.paid_at >= (@from AT TIME ZONE @tz) AND o.paid_at < (@toExclusive AT TIME ZONE @tz)";

        var summary = await conn.QuerySingleAsync<SummaryRow>(new CommandDefinition($"""
            SELECT count(*)::int                                   AS order_count,
                   coalesce(sum(o.grand_total), 0)                AS revenue,
                   coalesce(avg(o.grand_total), 0)                AS average_order_value,
                   coalesce((SELECT sum(round(oi.line_total * oi.commission_rate / 100, 2))
                               FROM order_items oi JOIN orders o ON o.id = oi.order_id
                              WHERE o.status IN {SoldStatuses} AND {range}), 0) AS commission
              FROM orders o
             WHERE o.status IN {SoldStatuses} AND {range}
            """, args, cancellationToken: ct));

        var newCustomers = await conn.ExecuteScalarAsync<int>(new CommandDefinition("""
            SELECT count(*)::int FROM users u
             WHERE u.created_at >= (@from AT TIME ZONE @tz) AND u.created_at < (@toExclusive AT TIME ZONE @tz)
            """, args, cancellationToken: ct));

        var failureRate = await conn.ExecuteScalarAsync<decimal?>(new CommandDefinition("""
            SELECT round(100.0 * count(*) FILTER (WHERE p.status = 3) / nullif(count(*) FILTER (WHERE p.status IN (2,3)), 0), 2)
              FROM payments p
             WHERE p.created_at >= (@from AT TIME ZONE @tz) AND p.created_at < (@toExclusive AT TIME ZONE @tz)
            """, args, cancellationToken: ct)) ?? 0;

        var daily = (await conn.QueryAsync<DailyRow>(new CommandDefinition($"""
            SELECT d::date AS day, count(o.id)::int AS order_count, coalesce(sum(o.grand_total), 0) AS revenue
              FROM generate_series(@from::timestamp, @toExclusive::timestamp - interval '1 day', interval '1 day') AS d  -- yerel gün başları (saat dilimsiz)
              LEFT JOIN orders o
                ON o.status IN {SoldStatuses}
               AND o.paid_at >= (d AT TIME ZONE @tz) AND o.paid_at < ((d + interval '1 day') AT TIME ZONE @tz)
             GROUP BY d ORDER BY d
            """, args, cancellationToken: ct)))
            .Select(r => new DailySalesRow(r.Day, r.OrderCount, r.Revenue)).ToList();

        var categories = (await conn.QueryAsync<CategorySalesRow>(new CommandDefinition($"""
            SELECT coalesce(parent.name, c.name) AS category, sum(oi.quantity)::int AS quantity, sum(oi.line_total) AS revenue
              FROM order_items oi
              JOIN orders o            ON o.id = oi.order_id
              JOIN product_variants v  ON v.id = oi.variant_id
              JOIN products p          ON p.id = v.product_id
              JOIN categories c        ON c.id = p.category_id
              LEFT JOIN categories mid ON mid.id = c.parent_id
              LEFT JOIN categories parent ON parent.id = coalesce(mid.parent_id, mid.id)
             WHERE o.status IN {SoldStatuses} AND {range}
             GROUP BY 1 ORDER BY revenue DESC LIMIT 10
            """, args, cancellationToken: ct))).ToList();

        var sellers = (await conn.QueryAsync<SellerSalesRow>(new CommandDefinition($"""
            SELECT s.id AS seller_id, s.store_name, count(DISTINCT o.id)::int AS order_count, sum(oi.line_total) AS revenue,
                   sum(round(oi.line_total * oi.commission_rate / 100, 2)) AS commission
              FROM order_items oi
              JOIN orders o  ON o.id = oi.order_id
              JOIN sellers s ON s.id = oi.seller_id
             WHERE o.status IN {SoldStatuses} AND {range}
             GROUP BY s.id, s.store_name ORDER BY revenue DESC LIMIT 10
            """, args, cancellationToken: ct))).ToList();

        return new SalesReport(from, to,
            new SalesSummary(summary.OrderCount, summary.Revenue, Math.Round(summary.AverageOrderValue, 2), summary.Commission, newCustomers, failureRate),
            daily, categories, sellers);
    }

    private sealed record SummaryRow(int OrderCount, decimal Revenue, decimal AverageOrderValue, decimal Commission);
    private sealed record DailyRow(DateOnly Day, int OrderCount, decimal Revenue); // Npgsql date sütununu DateOnly olarak okur
}
