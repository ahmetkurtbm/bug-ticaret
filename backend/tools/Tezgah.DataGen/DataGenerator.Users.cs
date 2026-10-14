using Tezgah.Application.Common.Text;
using Tezgah.DataGen.Data;
using Tezgah.Domain.Sellers;
using Tezgah.Infrastructure.Security;

namespace Tezgah.DataGen;

public partial class DataGenerator
{
    private async Task WriteRolesAndUsersAsync()
    {
        await using (var roles = await BulkWriter.OpenAsync(dataSource, "roles", "id", "name", "display_name"))
        {
            await roles.RowAsync(1L, "customer", "Müşteri");
            await roles.RowAsync(2L, "seller", "Satıcı");
            await roles.RowAsync(3L, "admin", "Yönetici");
        }

        // Herkese aynı demo şifresi: PBKDF2 kasıtlı yavaş, binlerce kullanıcıya ayrı ayrı hesaplanmaz.
        var passwordHash = new Pbkdf2PasswordHasher().Hash(DemoPassword);
        var usedEmails = new HashSet<string>();
        long nextUserId = 1;

        await using var users = await BulkWriter.OpenAsync(dataSource, "users",
            "id", "email", "normalized_email", "password_hash", "first_name", "last_name", "phone", "birth_date",
            "is_active", "last_login_at", "created_at");
        await using var userRoles = await BulkWriter.OpenAsync(dataSource, "user_roles", "user_id", "role_id");
        await using var addresses = await BulkWriter.OpenAsync(dataSource, "addresses",
            "id", "user_id", "title", "full_name", "phone", "city", "district", "line", "postal_code", "is_default", "created_at");
        long nextAddressId = 1;

        async Task<long> AddUser(string email, string first, string last, long roleId, DateTime createdAt, bool withAddress)
        {
            var id = nextUserId++;
            var phone = MessyPhone();
            await users.RowAsync(id, email, SlugHelper.NormalizeEmail(email), passwordHash, first, last, phone,
                Chance(0.6) ? DateOnly.FromDateTime(_now.AddYears(-_rng.Next(18, 70)).AddDays(-_rng.Next(365))) : null,
                !Chance(0.01), Chance(0.7) ? RandomPast(60) : null, createdAt);
            await userRoles.RowAsync(id, roleId);

            if (withAddress)
            {
                var city = PickCity();
                var district = Pick(TurkishData.Districts[city]);
                var line = $"{Pick(TurkishData.Streets)} No:{_rng.Next(1, 150)} D:{_rng.Next(1, 30)}";
                var postal = Chance(0.85) ? $"{_rng.Next(1, 82):00}{_rng.Next(100, 1000)}" : null;
                var fullName = $"{first} {last}";
                await addresses.RowAsync(nextAddressId++, id, "Ev", fullName, phone, city, district, line, postal, true, createdAt);
                if (Chance(0.3))
                    await addresses.RowAsync(nextAddressId++, id, "İş", fullName, phone, city, Pick(TurkishData.Districts[city]),
                        $"{Pick(TurkishData.Streets)} No:{_rng.Next(1, 150)} Kat:{_rng.Next(1, 12)}", postal, false, createdAt);
                _customers.Add(new UserInfo(id, fullName, phone, city, district, line, postal));
            }
            return id;
        }

        // Sabit demo hesapları
        await AddUser("admin@tezgah.test", "Deniz", "Yönetici", 3, _now.AddYears(-3), withAddress: false);
        var demoSellerOwner = await AddUser("satici@tezgah.test", "Selin", "Satıcı", 2, _now.AddYears(-2), withAddress: false);
        await AddUser("musteri@tezgah.test", "Mert", "Müşteri", 1, _now.AddYears(-1), withAddress: true);
        usedEmails.UnionWith(["admin@tezgah.test", "satici@tezgah.test", "musteri@tezgah.test"]);

        string UniqueEmail(string first, string last, string domain)
        {
            var baseName = $"{AsciiEmailPart(first)}.{AsciiEmailPart(last)}";
            var email = $"{baseName}@{domain}";
            var n = 1;
            while (!usedEmails.Add(email)) email = $"{baseName}{++n}@{domain}";
            return email;
        }

        // Satıcı hesapları
        var sellerOwners = new List<long> { demoSellerOwner };
        for (var i = 1; i < _p.Sellers; i++)
        {
            var first = _faker.Name.FirstName();
            var last = _faker.Name.LastName();
            sellerOwners.Add(await AddUser(UniqueEmail(first, last, "satici.test"), first, last, 2, RandomPast(1500, 200), withAddress: false));
        }

        // Müşteriler
        string[] domains = ["posta.test", "mail.test", "eposta.test"];
        for (var i = 0; i < _p.Customers; i++)
        {
            var first = _faker.Name.FirstName();
            var last = _faker.Name.LastName();
            await AddUser(UniqueEmail(first, last, Pick(domains)), first, last, 1, RandomPast(1200), withAddress: true);
        }

        await users.DisposeAsync();
        await userRoles.DisposeAsync();
        await addresses.DisposeAsync();

        await WriteSellersAsync(sellerOwners);
    }

    private async Task WriteSellersAsync(List<long> owners)
    {
        await using var sellers = await BulkWriter.OpenAsync(dataSource, "sellers",
            "id", "owner_user_id", "store_name", "slug", "legal_name", "tax_number", "tax_office", "iban", "city",
            "status", "commission_rate", "rating", "rating_count", "approved_at", "created_at");
        await using var warehouses = await BulkWriter.OpenAsync(dataSource, "warehouses", "id", "seller_id", "name", "city", "is_active");

        string[] suffixes = ["Store", "Market", "Butik", "Shop", "Dünyası", "Center", "Home", "Moda"];
        var slugs = new HashSet<string>();
        for (var i = 0; i < owners.Count; i++)
        {
            var id = (long)i + 1;
            var name = i == 0 ? "Tezgah Demo Mağaza" : $"{_faker.Name.LastName()} {Pick(suffixes)}";
            var slug = SlugHelper.Slugify(name);
            var n = 1;
            while (!slugs.Add(slug)) slug = $"{SlugHelper.Slugify(name)}-{++n}";

            var status = i == 0 ? SellerStatus.Active : _rng.NextDouble() switch
            {
                < 0.86 => SellerStatus.Active,
                < 0.93 => SellerStatus.Pending,
                < 0.98 => SellerStatus.Suspended,
                _ => SellerStatus.Closed,
            };
            var created = RandomPast(1500, 200);
            var commission = (decimal)Pick(new[] { 8.0, 10.0, 12.5, 15.0, 18.0 });
            var city = PickCity();

            await sellers.RowAsync(id, owners[i], name, slug, $"{name} Tic. Ltd. Şti.", (1_000_000_000L + id * 7919).ToString(),
                Pick(TurkishData.TaxOffices), Chance(0.9) ? $"TR{_rng.Next(10, 99)}000{_rng.NextInt64(1_000_000_000_000, 9_999_999_999_999)}000" : null,
                city, (int)status, commission, Math.Round((decimal)(3.2 + _rng.NextDouble() * 1.8), 1), _rng.Next(0, 5000),
                status == SellerStatus.Pending ? null : created.AddDays(_rng.Next(1, 10)), created);

            await warehouses.RowAsync(id, id, "Ana Depo", city, true);
            if (status == SellerStatus.Active)
                _sellers.Add(new SellerInfo(id, owners[i], commission, id, city));
        }
    }
}
