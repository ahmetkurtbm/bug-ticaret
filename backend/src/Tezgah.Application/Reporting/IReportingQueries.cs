namespace Tezgah.Application.Reporting;

public record SalesSummary(int OrderCount, decimal Revenue, decimal AverageOrderValue, decimal Commission, int NewCustomers, decimal PaymentFailureRate);
public record DailySalesRow(DateOnly Day, int OrderCount, decimal Revenue);
public record CategorySalesRow(string Category, int Quantity, decimal Revenue);
public record SellerSalesRow(long SellerId, string StoreName, int OrderCount, decimal Revenue, decimal Commission);
public record SalesReport(DateOnly From, DateOnly To, SalesSummary Summary, IReadOnlyList<DailySalesRow> Daily,
    IReadOnlyList<CategorySalesRow> TopCategories, IReadOnlyList<SellerSalesRow> TopSellers);

/// <summary>
/// Yönetim raporları. Eski raporlama modülünden taşındı: EF yerine elle yazılmış SQL (Dapper) kullanır,
/// çünkü sorgular büyük tablolarda toplama yapar ve ORM'in ürettiği SQL yavaştı.
/// </summary>
public interface IReportingQueries
{
    Task<SalesReport> GetSalesReportAsync(DateOnly from, DateOnly to, CancellationToken ct);
}
