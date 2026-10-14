using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Tezgah.DataGen;

// Kullanım:
//   dotnet run --project tools/Tezgah.DataGen -- --profile small --reset
//   dotnet run --project tools/Tezgah.DataGen -- --profile medium --seed 42 --reset
var options = GeneratorOptions.Parse(args);
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables("TEZGAH_")
    .Build();
var connectionString = options.ConnectionString
    ?? config.GetConnectionString("Tezgah")
    ?? "Host=localhost;Port=5440;Database=tezgah;Username=tezgah;Password=tezgah";

Console.WriteLine($"Profil: {options.Profile.Name} · tohum: {options.Seed}");
var total = Stopwatch.StartNew();

await using var dataSource = NpgsqlDataSource.Create(connectionString);
await using var conn = await dataSource.OpenConnectionAsync();

if (options.Reset)
    await Database.TruncateAllAsync(conn);
else if (await Database.HasDataAsync(conn))
{
    Console.WriteLine("Veritabanında zaten veri var. Sıfırlamak için --reset kullanın.");
    return 1;
}

var generator = new DataGenerator(conn, dataSource, options);
await generator.RunAsync();
await Database.ResetSequencesAsync(conn);

Console.WriteLine($"Bitti: {total.Elapsed:mm\\:ss}");
Console.WriteLine("Demo hesaplar (şifre: Tezgah123!): admin@tezgah.test · satici@tezgah.test · musteri@tezgah.test");
return 0;
