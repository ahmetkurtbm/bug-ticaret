using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Tezgah.Bolum;

/// <summary>
/// Her bölümün kontrolü. Kontrol hatanın kendisini değil, kullanıcının gördüğü davranışı sınar:
/// hata kaydındaki senaryo artık doğru çalışıyor mu? Aynı kontrol main dalında her zaman geçer.
/// </summary>
public static partial class Checks
{
    public static Func<CheckContext, Task<bool>> For(string id) => id switch
    {
        "ort01" => ContainerPaymentAsync,
        "fe01" => c => FrontendTestAsync(c, "storefront", "fe01"),
        "fe12" => SpaRefreshAsync,
        "api01" => c => FrontendTestAsync(c, "seller", "api01"),
        "api03" => SuspendedSellerAsync,
        "net01" => CorsAsync,
        "net02" => NotificationsApiKeyAsync,
        "net03" => OldOrderDetailAsync,
        "fe02" => c => FrontendTestAsync(c, "storefront", "fe02"),
        "api02" => c => FrontendTestAsync(c, "storefront", "api02"),
        "fe03" => c => FrontendTestAsync(c, "storefront", "fe03"),
        "fe14" => c => FrontendTestAsync(c, "seller", "fe14"),
        "net09" => TurkishLoginAsync,
        "net11" => CampaignDayBoundaryAsync,
        "ef01" => AdminOrderQueriesAsync,
        "arac01" => CampaignHistoryOrderAsync,
        "db01" => OrderHistoryPlanAsync,
        "es01" => LastStockRaceAsync,
        "es02" => DuplicateWebhookAsync,
        _ => throw new BolumException($"Bilinmeyen kontrol: {id}"),
    };

    // ------------------------------------------------------------------ ortak
    private static async Task<bool> FrontendTestAsync(CheckContext c, string app, string filter)
    {
        var frontend = Path.Combine(c.Repo.Root, "frontend");
        if (!Directory.Exists(Path.Combine(frontend, "node_modules")))
        {
            Output.Info("frontend bağımlılıkları kuruluyor (npm install)…");
            Shell.Npm("install --no-audit --no-fund", frontend);
        }
        Output.Info($"npm run test:bolum -w {app} -- {filter}");
        var result = Shell.Npm($"run test:bolum -w {app} -- {filter}", frontend, echo: false);
        var summary = result.Output.Split('\n').Where(l => l.Contains("✓") || l.Contains("×") || l.Contains("FAIL") || l.Contains("AssertionError") || l.Contains("Error:") || l.Contains("Tests ")).Take(12);
        foreach (var line in summary) Console.WriteLine("    " + line.Trim());
        if (result.ExitCode == 0)
        {
            Output.Pass("Bölüm testi geçti.");
            return true;
        }
        Output.Fail("Bölüm testi başarısız. Ayrıntı: npm run test:bolum -w " + app + " -- " + filter);
        return false;
    }

    private static async Task<(ApiSession Session, string OrderNumber, string? CheckoutUrl, string? PaymentError)?> PlaceOrderAsync(CheckContext c, string email, long variantId, int quantity = 1)
    {
        var session = await c.RequireLoginAsync(email);
        var address = await session.EnsureAddressAsync();
        if (!await session.PrepareCartAsync(variantId, quantity))
        {
            Output.Fail("Ürün sepete eklenemedi.");
            return null;
        }
        var placed = await session.PostAsync("/api/orders", new { addressId = address, installmentCount = 1 },
            new Dictionary<string, string> { ["Idempotency-Key"] = Guid.NewGuid().ToString() });
        if (placed.Status != 200)
        {
            Output.Fail($"Sipariş oluşturulamadı ({placed.Status}): {placed.Body?["title"]}");
            return null;
        }
        return (session, placed.Body!["orderNumber"]!.GetValue<string>(), placed.Body["checkoutUrl"]?.GetValue<string>(), placed.Body["paymentError"]?.GetValue<string>());
    }

    private static Task<long> CancelTestOrderAsync(CheckContext c, string orderNumber) =>
        c.ScalarAsync<long>("SELECT id FROM orders WHERE order_number = @n", ("n", orderNumber));

    // ------------------------------------------------------------------ ORT-01
    private static async Task<bool> ContainerPaymentAsync(CheckContext c)
    {
        var docker = Shell.Run("docker", "compose --profile app ps --format {{.Service}}:{{.State}}", c.Repo.Root);
        if (!docker.Output.Contains("api:running") || !docker.Output.Contains("payments:running"))
        {
            Output.Fail("Bu bölüm sistemin container'da çalışmasını ister: docker compose --profile app up -d --build");
            return false;
        }
        Output.Pass("api ve payments container'ları çalışıyor.");
        if (!await c.ApiIsUpAsync()) return false;

        var variant = await c.FindSellableVariantAsync(3);
        var order = await PlaceOrderAsync(c, "musteri@tezgah.test", variant);
        if (order is null) return false;
        if (order.Value.CheckoutUrl is null)
        {
            Output.Fail($"Sipariş {order.Value.OrderNumber} oluştu ama ödeme başlamadı: {order.Value.PaymentError}");
            Output.Info("API container'ının loglarına bakın: docker compose logs api");
            return false;
        }
        Output.Pass($"Ödeme oturumu açıldı: {order.Value.CheckoutUrl}");
        return true;
    }

    // ------------------------------------------------------------------ FE-12
    private static async Task<bool> SpaRefreshAsync(CheckContext c)
    {
        string body;
        int status;
        try
        {
            using var response = await c.Http.GetAsync($"{c.StorefrontUrl}/hesabim/siparisler");
            status = (int)response.StatusCode;
            body = await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException)
        {
            Output.Fail("Müşteri sitesine ulaşılamadı. Container'ı başlatın: docker compose --profile app up -d --build storefront");
            return false;
        }

        if (body.Contains("/@vite/client"))
        {
            Output.Fail("5173 portunda Vite geliştirme sunucusu çalışıyor; o her adrese index.html döner ve hata görünmez.");
            Output.Info("Geliştirme sunucusunu kapatıp container'ı başlatın: docker compose --profile app up -d --build storefront");
            return false;
        }

        var ok = status == 200 && body.Contains("id=\"root\"");
        if (ok) Output.Pass("/hesabim/siparisler doğrudan açılınca uygulama yükleniyor (200).");
        else Output.Fail($"/hesabim/siparisler doğrudan açılınca {status} döndü.");

        using var asset = await c.Http.GetAsync($"{c.StorefrontUrl}/assets/olmayan-dosya-123.js");
        var assetOk = (int)asset.StatusCode == 404;
        if (assetOk) Output.Pass("Olmayan bir dosya hâlâ 404 dönüyor.");
        else Output.Fail($"Olmayan bir .js dosyası {(int)asset.StatusCode} döndü; her şeyi index.html'e yönlendirmek de hatadır.");
        return ok && assetOk;
    }

    // ------------------------------------------------------------------ API-03
    private static async Task<bool> SuspendedSellerAsync(CheckContext c)
    {
        if (!await c.ApiIsUpAsync()) return false;
        var admin = await c.RequireLoginAsync("admin@tezgah.test");
        var email = $"kontrol.satici.{Guid.NewGuid():N}"[..28] + "@tezgah.test";
        var customer = await c.RegisterAsync(email);
        var tax = Random.Shared.NextInt64(1_000_000_000, 9_999_999_999).ToString(CultureInfo.InvariantCulture);
        var applied = await customer.PostAsync("/api/seller-applications", new
        {
            storeName = "Kontrol Mağazası " + tax[..4], legalName = "Kontrol Ltd", taxNumber = tax, taxOffice = "Kadıköy",
            iban = "TR330006100519786457841326", city = "İstanbul",
        });
        if (applied.Status != 200) { Output.Fail($"Satıcı başvurusu yapılamadı ({applied.Status})."); return false; }
        var sellerId = applied.Body!["id"]!.GetValue<long>();
        await admin.PostAsync($"/api/admin/sellers/{sellerId}/approve");
        var seller = await c.RequireLoginAsync(email);

        var active = await seller.GetAsync("/api/seller/dashboard");
        if (active.Status != 200) { Output.Fail($"Aktif mağaza paneli açamadı ({active.Status})."); return false; }
        Output.Pass("Aktif mağaza paneli açıyor (200).");

        await admin.PostAsync($"/api/admin/sellers/{sellerId}/suspend", new { note = "Bölüm kontrolü" });
        var suspended = await seller.GetAsync("/api/seller/dashboard");
        if (suspended.Status == 403)
        {
            Output.Pass($"Askıdaki mağaza 403 alıyor: \"{suspended.Body?["title"]}\"");
            return true;
        }
        Output.Fail($"Askıdaki mağaza {suspended.Status} aldı. Kimliği belli ama yetkisi olmayan kullanıcıya hangi kod dönmeli?");
        return false;
    }

    // ------------------------------------------------------------------ NET-01
    private static async Task<bool> CorsAsync(CheckContext c)
    {
        if (!await c.ApiIsUpAsync()) return false;
        var all = true;
        foreach (var origin in new[] { "http://localhost:5173", "http://localhost:5174", "http://localhost:5175" })
        {
            using var request = new HttpRequestMessage(HttpMethod.Options, $"{c.ApiUrl}/api/seller/dashboard");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "GET");
            request.Headers.Add("Access-Control-Request-Headers", "authorization");
            using var response = await c.Http.SendAsync(request);
            var allowed = response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values) ? values.FirstOrDefault() : null;
            if (allowed == origin) Output.Pass($"{origin} için ön kontrol (preflight) izin veriyor.");
            else { Output.Fail($"{origin} için Access-Control-Allow-Origin: {allowed ?? "(yok)"}"); all = false; }
        }
        return all;
    }

    // ------------------------------------------------------------------ NET-02
    private static async Task<bool> NotificationsApiKeyAsync(CheckContext c)
    {
        try
        {
            using var health = await c.Http.GetAsync($"{c.NotificationsUrl}/health");
            if (!health.IsSuccessStatusCode) { Output.Fail($"/health {(int)health.StatusCode} döndü; anahtarsız açık olmalı."); return false; }
            Output.Pass("/health anahtarsız açık (200).");
        }
        catch (HttpRequestException)
        {
            Output.Fail($"Bildirim servisine ulaşılamadı ({c.NotificationsUrl}).");
            return false;
        }

        using var anonymous = await c.Http.GetAsync($"{c.NotificationsUrl}/api/notifications");
        var anonOk = (int)anonymous.StatusCode == 401;
        if (anonOk) Output.Pass("Anahtarsız /api/notifications 401.");
        else Output.Fail($"Anahtarsız /api/notifications {(int)anonymous.StatusCode} döndü; müşteri e-postaları dışarı açık!");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{c.NotificationsUrl}/api/notifications");
        request.Headers.Add("X-Api-Key", c.NotificationsApiKey);
        using var withKey = await c.Http.SendAsync(request);
        var keyOk = withKey.IsSuccessStatusCode;
        if (keyOk) Output.Pass("Doğru anahtarla /api/notifications 200.");
        else Output.Fail($"Doğru anahtarla {(int)withKey.StatusCode} döndü.");
        return anonOk && keyOk;
    }

    // ------------------------------------------------------------------ NET-03
    private static async Task<bool> OldOrderDetailAsync(CheckContext c)
    {
        if (!await c.ApiIsUpAsync()) return false;
        await using var conn = await c.OpenDbAsync();
        await using var cmd = new Npgsql.NpgsqlCommand("""
            SELECT o.order_number, u.email FROM orders o JOIN users u ON u.id = o.user_id
             WHERE o.shipping_full_name IS NULL ORDER BY o.placed_at LIMIT 1
            """, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) { Output.Fail("Adres bilgisi olmayan eski sipariş bulunamadı; örnek veriyi yeniden üretin."); return false; }
        var (orderNumber, email) = (reader.GetString(0), reader.GetString(1));
        Output.Info($"Eski sipariş: {orderNumber} ({email})");

        var session = await c.RequireLoginAsync(email);
        var detail = await session.GetAsync($"/api/orders/{orderNumber}");
        if (detail.Status == 200)
        {
            Output.Pass("Eski siparişin detayı açılıyor; adres yoksa boş geliyor.");
            var list = await session.GetAsync("/api/orders?page=1&pageSize=50");
            if (list.Status != 200) { Output.Fail($"Sipariş listesi {list.Status} döndü."); return false; }
            Output.Pass("Sipariş listesi açılıyor.");
            return true;
        }
        Output.Fail($"Eski siparişin detayı {detail.Status} döndü.");
        return false;
    }

    // ------------------------------------------------------------------ NET-09
    private static async Task<bool> TurkishLoginAsync(CheckContext c)
    {
        if (!await c.ApiIsUpAsync()) return false;
        var all = true;
        foreach (var email in new[] { "admin@tezgah.test", "satici@tezgah.test", "musteri@tezgah.test", "MUSTERI@tezgah.test" })
        {
            if (await c.LoginAsync(email) is not null) Output.Pass($"{email} giriş yapabiliyor.");
            else { Output.Fail($"{email} giriş yapamıyor."); all = false; }
        }
        var fresh = $"ilkim.{Guid.NewGuid():N}"[..14] + "@tezgah.test";
        await c.RegisterAsync(fresh);
        if (await c.LoginAsync(fresh.ToUpperInvariant()) is not null) Output.Pass("Yeni kayıt büyük harfle yazılan e-postayla da giriş yapabiliyor.");
        else { Output.Fail("Yeni kayıt, e-postası büyük harfle yazılınca giriş yapamıyor."); all = false; }
        return all;
    }

    // ------------------------------------------------------------------ NET-11
    private static async Task<bool> CampaignDayBoundaryAsync(CheckContext c)
    {
        if (!await c.ApiIsUpAsync()) return false;
        var admin = await c.RequireLoginAsync("admin@tezgah.test");
        var istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, istanbul).Date);
        var start = today.AddDays(7);
        var end = start.AddDays(2);
        var code = "KONTROL" + Random.Shared.Next(100000, 999999).ToString(CultureInfo.InvariantCulture);
        var created = await admin.PostAsync("/api/admin/coupons", new
        {
            code, description = "Bölüm kontrolü", type = 1, value = 10, minOrderAmount = 0,
            startsOn = start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), endsOn = end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        });
        if (created.Status != 200) { Output.Fail($"Kupon oluşturulamadı ({created.Status})."); return false; }
        var id = created.Body!["id"]!.GetValue<long>();
        await admin.PostAsync($"/api/admin/coupons/{id}/deactivate");

        DateTimeOffset Expected(DateOnly d) => new DateTimeOffset(d.ToDateTime(TimeOnly.MinValue), istanbul.GetUtcOffset(d.ToDateTime(TimeOnly.MinValue))).ToUniversalTime();
        var startsAt = DateTimeOffset.Parse(created.Body["startsAt"]!.GetValue<string>(), CultureInfo.InvariantCulture);
        var endsAt = DateTimeOffset.Parse(created.Body["endsAt"]!.GetValue<string>(), CultureInfo.InvariantCulture);
        var okStart = startsAt == Expected(start);
        var okEnd = endsAt == Expected(end.AddDays(1));
        if (okStart) Output.Pass($"Başlangıç İstanbul saatiyle {start:dd.MM.yyyy} 00:00 ({startsAt:u}).");
        else Output.Fail($"Başlangıç {TimeZoneInfo.ConvertTime(startsAt, istanbul):dd.MM.yyyy HH:mm} (İstanbul) olmuş; {start:dd.MM.yyyy} 00:00 bekleniyordu.");
        if (okEnd) Output.Pass($"Bitiş {end:dd.MM.yyyy} gününün sonu (İstanbul).");
        else Output.Fail($"Bitiş {TimeZoneInfo.ConvertTime(endsAt, istanbul):dd.MM.yyyy HH:mm} (İstanbul) olmuş; {end.AddDays(1):dd.MM.yyyy} 00:00 bekleniyordu.");
        return okStart && okEnd;
    }

    // ------------------------------------------------------------------ EF-01
    private static async Task<bool> AdminOrderQueriesAsync(CheckContext c)
    {
        if (!await c.ApiIsUpAsync()) return false;
        var admin = await c.RequireLoginAsync("admin@tezgah.test");
        var watch = Stopwatch.StartNew();
        var list = await admin.GetAsync("/api/admin/orders?page=1&pageSize=50");
        watch.Stop();
        if (list.Status != 200) { Output.Fail($"Sipariş listesi {list.Status} döndü."); return false; }
        var timing = list.Response.Headers.TryGetValues("Server-Timing", out var values) ? values.FirstOrDefault() : null;
        var match = timing is null ? null : QueryCountRegex().Match(timing);
        if (match is not { Success: true })
        {
            Output.Fail("Yanıtta Server-Timing başlığı yok. API Development ortamında mı çalışıyor?");
            return false;
        }
        var queries = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        Output.Info($"50 sipariş: {queries} SQL sorgusu, {watch.ElapsedMilliseconds} ms ({timing})");
        if (queries <= 5) { Output.Pass("Sorgu sayısı satır sayısından bağımsız."); return true; }
        Output.Fail("Sayfadaki her sipariş için ayrı sorgu çalışıyor (N+1).");
        return false;
    }

    [GeneratedRegex("desc=\"(\\d+) sorgu\"")]
    private static partial Regex QueryCountRegex();

    // ------------------------------------------------------------------ ARAÇ-01
    private static async Task<bool> CampaignHistoryOrderAsync(CheckContext c)
    {
        if (!await c.ApiIsUpAsync()) return false;
        var admin = await c.RequireLoginAsync("admin@tezgah.test");
        var code = "GECMIS" + Random.Shared.Next(100000, 999999).ToString(CultureInfo.InvariantCulture);
        var created = await admin.PostAsync("/api/admin/coupons", new
        {
            code, description = "Bölüm kontrolü", type = 1, value = 5, minOrderAmount = 0, startsOn = "2026-01-01", endsOn = "2026-01-02",
        });
        if (created.Status != 200) { Output.Fail($"Kupon oluşturulamadı ({created.Status})."); return false; }
        var id = created.Body!["id"]!.GetValue<long>();
        await admin.PostAsync($"/api/admin/coupons/{id}/deactivate");
        await admin.PostAsync($"/api/admin/coupons/{id}/activate");
        await admin.PostAsync($"/api/admin/coupons/{id}/deactivate");

        var history = await admin.GetAsync("/api/admin/campaign-history?take=20");
        var entries = history.Body?.AsArray().Select(e => (At: DateTimeOffset.Parse(e!["at"]!.GetValue<string>(), CultureInfo.InvariantCulture), Action: e["action"]!.GetValue<string>(), Code: e["couponCode"]?.GetValue<string>(), Actor: e["actorEmail"]?.GetValue<string>())).ToList() ?? [];
        if (entries.Count == 0) { Output.Fail("Kampanya geçmişi boş döndü."); return false; }

        var all = true;
        var first = entries[0];
        if (first.Code == code && first.Action == "coupon.deactivated") Output.Pass("En son işlem listenin başında.");
        else { Output.Fail($"Listenin başında {first.Code} / {first.Action} var; az önce yapılan {code} / coupon.deactivated bekleniyordu."); all = false; }
        var sorted = entries.Zip(entries.Skip(1)).All(p => p.First.At >= p.Second.At);
        if (sorted) Output.Pass("Kayıtlar yeniden eskiye sıralı.");
        else { Output.Fail("Kayıtlar tarihe göre yeniden eskiye sıralı değil."); all = false; }
        if (first.Actor == "admin@tezgah.test") Output.Pass("İşlemi yapan yönetici görünüyor.");
        else { Output.Fail($"İşlemi yapan: {first.Actor ?? "(boş)"}; admin@tezgah.test bekleniyordu."); all = false; }
        return all;
    }

    // ------------------------------------------------------------------ DB-01
    private static async Task<bool> OrderHistoryPlanAsync(CheckContext c)
    {
        var orders = await c.ScalarAsync<long>("SELECT count(*) FROM orders");
        Output.Info($"orders tablosunda {orders:N0} satır.");
        if (orders < 500_000) Output.Info("Yavaşlığı hissetmek için orta profil önerilir: DataGen --profile medium --reset");

        var userId = await c.ScalarAsync<long>("SELECT user_id FROM orders GROUP BY user_id ORDER BY count(*) DESC LIMIT 1");
        await using var conn = await c.OpenDbAsync();
        await using (var analyze = new Npgsql.NpgsqlCommand("ANALYZE orders", conn)) await analyze.ExecuteNonQueryAsync();
        // "Siparişlerim" sayfasının çalıştırdığı sorgunun özü.
        await using var cmd = new Npgsql.NpgsqlCommand($"""
            EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
            SELECT o.id, o.order_number, o.status, o.placed_at, o.grand_total
              FROM orders o WHERE o.user_id = {userId}
             ORDER BY o.placed_at DESC, o.id DESC LIMIT 10
            """, conn);
        var plan = new StringBuilder();
        await using (var reader = await cmd.ExecuteReaderAsync())
            while (await reader.ReadAsync()) plan.AppendLine(reader.GetString(0));
        var text = plan.ToString();
        foreach (var line in text.Split('\n').Take(8)) if (line.Trim().Length > 0) Console.WriteLine("    " + line.TrimEnd());

        if (text.Contains("Seq Scan on orders", StringComparison.Ordinal))
        {
            Output.Fail("Sorgu orders tablosunun tamamını tarıyor (Seq Scan).");
            return false;
        }
        Output.Pass("Sorgu index kullanıyor; tablonun tamamı taranmıyor.");
        return true;
    }

    // ------------------------------------------------------------------ EŞ-01
    private static async Task<bool> LastStockRaceAsync(CheckContext c)
    {
        if (!await c.ApiIsUpAsync()) return false;
        const int buyers = 8;
        var variant = await c.FindSellableVariantAsync(1);
        var stockItemId = await c.ScalarAsync<long>("SELECT id FROM stock_items WHERE variant_id = @v", ("v", variant));
        var original = await c.ScalarAsync<string>("SELECT quantity || ':' || reserved FROM stock_items WHERE id = @id", ("id", stockItemId));
        var emails = await c.CustomerEmailsAsync(buyers);

        // Hazırlık: her alıcının sepetinde bu üründen 1 adet; sonra stokta yalnızca 1 adet bırak.
        var sessions = new List<(ApiSession Session, long Address)>();
        foreach (var email in emails)
        {
            var s = await c.RequireLoginAsync(email);
            var address = await s.EnsureAddressAsync();
            if (!await s.PrepareCartAsync(variant, 1)) { Output.Fail("Ürün sepete eklenemedi."); return false; }
            sessions.Add((s, address));
        }
        await c.ExecuteAsync("UPDATE stock_items SET quantity = reserved + 1 WHERE id = @id", ("id", stockItemId));
        Output.Info($"Stokta 1 adet kaldı; {buyers} müşteri aynı anda sipariş veriyor…");

        var created = new List<string>();
        try
        {
            var gate = new TaskCompletionSource();
            var tasks = sessions.Select(async x =>
            {
                await gate.Task;
                return await x.Session.PostAsync("/api/orders", new { addressId = x.Address, installmentCount = 1 },
                    new Dictionary<string, string> { ["Idempotency-Key"] = Guid.NewGuid().ToString() });
            }).ToList();
            gate.SetResult();
            var results = await Task.WhenAll(tasks);
            created = results.Where(r => r.Status == 200).Select(r => r.Body!["orderNumber"]!.GetValue<string>()).ToList();
            var rejected = results.Count(r => r.Status == 409);
            var others = results.Where(r => r.Status is not 200 and not 409).Select(r => r.Status).ToList();

            var state = await c.ScalarAsync<string>("SELECT quantity || ':' || reserved FROM stock_items WHERE id = @id", ("id", stockItemId));
            var parts = state!.Split(':').Select(int.Parse).ToArray();
            Output.Info($"Başarılı sipariş: {created.Count}, stok tükendi (409): {rejected}{(others.Count > 0 ? ", diğer: " + string.Join(",", others) : "")}; stok (adet:ayrılan) {state}");
            var ok = created.Count == 1 && parts[1] <= parts[0];
            if (ok) Output.Pass("Son ürünü yalnızca bir müşteri aldı.");
            else Output.Fail(created.Count > 1 ? $"Tek kalan ürün {created.Count} müşteriye satıldı." : "Hiçbir müşteri siparişi tamamlayamadı.");
            return ok;
        }
        finally
        {
            // Temizlik: kontrolün açtığı siparişleri iptal et, stoğu eski hâline getir.
            foreach (var number in created)
                await c.ExecuteAsync("UPDATE orders SET status = 6, cancelled_at = now() WHERE order_number = @n AND status = 1", ("n", number));
            var o = original!.Split(':').Select(int.Parse).ToArray();
            await c.ExecuteAsync("UPDATE stock_items SET quantity = @q, reserved = @r WHERE id = @id", ("q", o[0]), ("r", o[1]), ("id", stockItemId));
        }
    }

    // ------------------------------------------------------------------ EŞ-02
    private static async Task<bool> DuplicateWebhookAsync(CheckContext c)
    {
        if (!await c.ApiIsUpAsync()) return false;
        var variant = await c.FindSellableVariantAsync(20);
        var stockItemId = await c.ScalarAsync<long>("SELECT id FROM stock_items WHERE variant_id = @v", ("v", variant));
        var original = (await c.ScalarAsync<string>("SELECT quantity || ':' || reserved FROM stock_items WHERE id = @id", ("id", stockItemId)))!.Split(':').Select(int.Parse).ToArray();
        var order = await PlaceOrderAsync(c, "musteri@tezgah.test", variant, 2);
        if (order is null) return false;
        var (_, orderNumber, checkoutUrl, _) = order.Value;
        if (checkoutUrl is null) { Output.Fail("Ödeme başlatılamadı; ödeme servisi çalışıyor mu?"); return false; }

        var info = await c.ScalarAsync<string>("SELECT id || ':' || grand_total::text FROM orders WHERE order_number = @n", ("n", orderNumber));
        var orderId = long.Parse(info!.Split(':')[0], CultureInfo.InvariantCulture);
        var amount = decimal.Parse(info.Split(':')[1], CultureInfo.InvariantCulture);
        var stockBefore = await c.ScalarAsync<int>("SELECT quantity FROM stock_items WHERE variant_id = @v", ("v", variant));
        var paymentId = Guid.Parse(checkoutUrl.TrimEnd('/').Split('/').Last());

        // Aynı üründen başka müşterilerin de ödeme bekleyen siparişi var (10 adet ayrılmış).
        await c.ExecuteAsync("UPDATE stock_items SET reserved = reserved + 10 WHERE id = @id", ("id", stockItemId));
        try
        {
        // Ödeme servisi aynı olayı (aynı EventId) ağ sorunu yüzünden 5 kez, neredeyse aynı anda gönderiyor.
        var evt = new
        {
            eventId = Guid.NewGuid(), paymentId, orderId, orderNumber, status = "succeeded", amount, currency = "TRY",
            installmentCount = 1, failureReason = (string?)null, occurredAt = DateTimeOffset.UtcNow,
        };
        var body = JsonSerializer.Serialize(evt, CheckContext.Json);
        var signature = Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(c.WebhookSecret), Encoding.UTF8.GetBytes(body)));
        var gate = new TaskCompletionSource();
        var sends = Enumerable.Range(0, 5).Select(async _ =>
        {
            await gate.Task;
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{c.ApiUrl}/api/payments/webhook") { Content = new StringContent(body, Encoding.UTF8, "application/json") };
            request.Headers.Add("X-Tezgah-Signature", signature);
            using var response = await c.Http.SendAsync(request);
            return (int)response.StatusCode;
        }).ToList();
        gate.SetResult();
        var statuses = await Task.WhenAll(sends);
        Output.Info($"Webhook yanıtları: {string.Join(", ", statuses)}");

        var stockAfter = await c.ScalarAsync<int>("SELECT quantity FROM stock_items WHERE variant_id = @v", ("v", variant));
        var reservedAfter = await c.ScalarAsync<int>("SELECT reserved FROM stock_items WHERE variant_id = @v", ("v", variant));
        var movements = await c.ScalarAsync<long>("SELECT count(*) FROM stock_movements WHERE reference_type = 'order' AND reference_id = @id", ("id", orderId));
        var paidHistory = await c.ScalarAsync<long>("SELECT count(*) FROM order_status_history WHERE order_id = @id AND to_status = 2", ("id", orderId));
        var status = await c.ScalarAsync<int>("SELECT status FROM orders WHERE id = @id", ("id", orderId));

        var all = true;
        if (status == 2) Output.Pass("Sipariş ödendi durumunda.");
        else { Output.Fail($"Sipariş durumu {status}; 2 (ödendi) bekleniyordu."); all = false; }
        if (stockBefore - stockAfter == 2) Output.Pass("Stok bir kez (2 adet) düştü.");
        else { Output.Fail($"Stok {stockBefore - stockAfter} adet düştü; 2 bekleniyordu."); all = false; }
        if (movements == 1 && paidHistory == 1) Output.Pass("Tek stok hareketi ve tek \"ödendi\" kaydı var.");
        else { Output.Fail($"{movements} stok hareketi ve {paidHistory} \"ödendi\" kaydı oluştu; birer tane bekleniyordu."); all = false; }
        if (reservedAfter == original[1] + 10) Output.Pass("Diğer müşterilerin ayırdığı 10 adet yerinde duruyor.");
        else { Output.Fail($"Diğer müşterilerin ayırdığı adet {reservedAfter - original[1]} oldu; 10 bekleniyordu (başkalarının rezervasyonu düşüldü)."); all = false; }
        if (statuses.All(s => s == 200)) Output.Pass("Tekrarlanan olaylar 200 ile yanıtlandı (gönderen tekrar denemeyi bırakır).");
        else { Output.Fail("Bazı tekrarlar 200 dışında yanıt aldı; gönderen yeniden denemeye devam eder."); all = false; }
        return all;
        }
        finally
        {
            // Temizlik: kontrolün eklediği sahte rezervasyonu geri al; satılan 2 adet satılmış kalır.
            await c.ExecuteAsync("UPDATE stock_items SET quantity = @q, reserved = @r WHERE id = @id",
                ("q", original[0] - 2), ("r", original[1]), ("id", stockItemId));
        }
    }
}
