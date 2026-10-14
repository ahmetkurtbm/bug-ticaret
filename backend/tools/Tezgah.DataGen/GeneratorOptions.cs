namespace Tezgah.DataGen;

public record Profile(string Name, int Customers, int Sellers, int Products, int Orders);

public class GeneratorOptions
{
    public static readonly Profile Small = new("small", Customers: 5_000, Sellers: 60, Products: 7_000, Orders: 50_000);
    public static readonly Profile Medium = new("medium", Customers: 100_000, Sellers: 600, Products: 100_000, Orders: 1_000_000);
    public static readonly Profile Large = new("large", Customers: 1_000_000, Sellers: 3_000, Products: 700_000, Orders: 10_000_000);

    public Profile Profile { get; init; } = Small;
    public int Seed { get; init; } = 20261110;
    public bool Reset { get; init; }
    public string? ConnectionString { get; init; }

    /// <summary>Verinin "bugün"ü. Aynı tohumla aynı veriyi üretmek için sabit tutulabilir.</summary>
    public DateTime Now { get; init; } = DateTime.UtcNow;

    public static GeneratorOptions Parse(string[] args)
    {
        var profile = Small;
        var seed = 20261110;
        var reset = false;
        string? cs = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--profile":
                    profile = args[++i] switch
                    {
                        "small" => Small,
                        "medium" => Medium,
                        "large" => Large,
                        var other => throw new ArgumentException($"Bilinmeyen profil: {other} (small | medium | large)"),
                    };
                    break;
                case "--seed":
                    seed = int.Parse(args[++i]);
                    break;
                case "--reset":
                    reset = true;
                    break;
                case "--connection":
                    cs = args[++i];
                    break;
                default:
                    throw new ArgumentException($"Bilinmeyen argüman: {args[i]}");
            }
        }
        return new GeneratorOptions { Profile = profile, Seed = seed, Reset = reset, ConnectionString = cs };
    }
}
