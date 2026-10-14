using Bogus;
using Npgsql;
using Tezgah.Application.Common.Text;
using Tezgah.Application.Pricing;
using Tezgah.DataGen.Data;
using Tezgah.Domain.Campaigns;
using Tezgah.Domain.Catalog;
using Tezgah.Domain.Inventory;
using Tezgah.Domain.Orders;
using Tezgah.Domain.Payments;
using Tezgah.Domain.Sellers;
using Tezgah.Domain.Shipping;
using Tezgah.Infrastructure.Security;

namespace Tezgah.DataGen;

/// <summary>
/// Pazar yeri verisini üretir. Sıra önemlidir: kimlikler elle verilir ve sonraki adımlar önceki
/// adımların bellekteki özetlerini kullanır.
/// </summary>
public partial class DataGenerator(NpgsqlConnection conn, NpgsqlDataSource dataSource, GeneratorOptions options)
{
    public const string DemoPassword = "Tezgah123!";

    private readonly Random _rng = new(options.Seed);
    private readonly Faker _faker = CreateFaker(options.Seed);
    private readonly DateTime _now = options.Now;
    private readonly Profile _p = options.Profile;

    // Önceki adımların bellekteki özetleri
    private readonly List<UserInfo> _customers = new();
    private readonly List<SellerInfo> _sellers = new();
    private readonly List<LeafInfo> _leaves = new();
    private readonly List<VariantInfo> _variants = new();
    private readonly List<CouponInfo> _coupons = new();
    private long[] _popularVariantIndexes = [];
    private long _brandCount;

    private static Faker CreateFaker(int seed)
    {
        Randomizer.Seed = new Random(seed);
        return new Faker("tr");
    }

    public async Task RunAsync()
    {
        Console.WriteLine("Kullanıcılar ve satıcılar…");
        await WriteRolesAndUsersAsync();
        Console.WriteLine("Katalog…");
        await WriteCategoriesAndBrandsAsync();
        await WriteProductsAsync();
        Console.WriteLine("Stok ve kampanyalar…");
        await WriteStockAsync();
        await WriteCouponsAsync();
        Console.WriteLine("Siparişler…");
        await WriteOrdersAsync();
        Console.WriteLine("Özet alanlar güncelleniyor…");
        await UpdateAggregatesAsync();
    }

    // ---------------------------------------------------------------- yardımcılar

    private record UserInfo(long Id, string FullName, string? Phone, string City, string District, string Line, string? PostalCode);
    private record SellerInfo(long Id, long OwnerUserId, decimal CommissionRate, long WarehouseId, string City);
    private record LeafInfo(long Id, LeafCategory Seed, long[] AttributeIds);
    private record VariantInfo(long Id, long ProductId, long SellerId, string ProductName, string Sku, string? Label, decimal Price, decimal VatRate, bool Active);
    private record CouponInfo(long Id, string Code, CouponType Type, decimal Value, decimal? MaxDiscount, decimal MinOrder, long? SellerId, DateTime StartsAt, DateTime EndsAt);

    private T Pick<T>(IReadOnlyList<T> items) => items[_rng.Next(items.Count)];
    private bool Chance(double p) => _rng.NextDouble() < p;
    private decimal RoundPrice(decimal value) => Math.Max(1, Math.Round(value / 10m) * 10m - 0.10m);

    private DateTime RandomPast(int maxDays, int minDays = 0) =>
        _now.AddDays(-_rng.Next(minDays, maxDays + 1)).AddSeconds(-_rng.Next(0, 86_400));

    private string PickCity()
    {
        var roll = (float)_rng.NextDouble();
        foreach (var (city, weight) in TurkishData.CityWeights)
        {
            if (roll < weight) return city;
            roll -= weight;
        }
        return "İstanbul";
    }

    /// <summary>Yıllar içinde farklı ekranlardan girilmiş gibi tutarsız telefon biçimleri (bilerek kirli).</summary>
    private string? MessyPhone()
    {
        if (Chance(0.06)) return null;
        var digits = $"5{_rng.Next(30, 60)}{_rng.Next(100, 1000)}{_rng.Next(1000, 10000)}";
        return _rng.Next(5) switch
        {
            0 => $"0{digits}",
            1 => $"+90 {digits[..3]} {digits[3..6]} {digits[6..8]} {digits[8..]}",
            2 => $"{digits[..3]} {digits[3..]}",
            3 => $"(0{digits[..3]}) {digits[3..6]} {digits[6..]}",
            _ => $"0{digits[..3]}-{digits[3..6]}-{digits[6..]}",
        };
    }

    private static string? VariantLabel(string? color, string? size) =>
        (color, size) switch
        {
            (null, null) => null,
            (not null, null) => color,
            (null, not null) => size,
            _ => $"{color} / {size}",
        };

    private static string AsciiEmailPart(string value) => SlugHelper.Slugify(value).Replace("-", ".");

    private async Task ExecAsync(string sql)
    {
        await using var cmd = new NpgsqlCommand(sql, conn) { CommandTimeout = 0 };
        await cmd.ExecuteNonQueryAsync();
    }
}
