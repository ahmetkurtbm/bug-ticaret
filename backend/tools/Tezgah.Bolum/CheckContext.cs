using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Npgsql;

namespace Tezgah.Bolum;

/// <summary>Kontrollerin ortak araçları: servis adresleri, giriş, veritabanı.</summary>
public class CheckContext(Repository repo)
{
    public const string DemoPassword = "Tezgah123!";
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Repository Repo { get; } = repo;
    public string ApiUrl { get; } = Env("TEZGAH_API_URL", "http://localhost:5080");
    public string PaymentsUrl { get; } = Env("TEZGAH_PAYMENTS_URL", "http://localhost:5090");
    public string NotificationsUrl { get; } = Env("TEZGAH_NOTIFICATIONS_URL", "http://localhost:5095");
    public string StorefrontUrl { get; } = Env("TEZGAH_STOREFRONT_URL", "http://localhost:5173");
    public string ConnectionString { get; } = Env("TEZGAH_DB", "Host=localhost;Port=5440;Database=tezgah;Username=tezgah;Password=tezgah");
    public string WebhookSecret { get; } = Env("TEZGAH_WEBHOOK_SECRET", "gelistirme-webhook-sirri-0123456789abcdef");
    public string NotificationsApiKey { get; } = Env("TEZGAH_NOTIFICATIONS_KEY", "gelistirme-bildirim-api-anahtari");

    public HttpClient Http { get; } = new() { Timeout = TimeSpan.FromSeconds(30) };

    private static string Env(string name, string fallback) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } v ? v : fallback;

    public async Task<bool> ApiIsUpAsync()
    {
        try
        {
            using var response = await Http.GetAsync($"{ApiUrl}/health");
            if (response.IsSuccessStatusCode) return true;
        }
        catch (HttpRequestException) { }
        Output.Fail($"API'ye ulaşılamadı ({ApiUrl}). Servisleri başlatın (bkz. docs/KURULUM.md).");
        return false;
    }

    public async Task<ApiSession?> LoginAsync(string email, string password = DemoPassword)
    {
        using var response = await Http.PostAsJsonAsync($"{ApiUrl}/api/auth/login", new { email, password });
        if (!response.IsSuccessStatusCode) return null;
        var body = await response.Content.ReadFromJsonAsync<JsonObject>(Json);
        return new ApiSession(this, body!["accessToken"]!.GetValue<string>());
    }

    public async Task<ApiSession> RequireLoginAsync(string email)
    {
        return await LoginAsync(email) ?? throw new BolumException($"{email} ile giriş yapılamadı. Örnek veri üretildi mi? (DataGen)");
    }

    public async Task<ApiSession> RegisterAsync(string email)
    {
        using var response = await Http.PostAsJsonAsync($"{ApiUrl}/api/auth/register", new { email, password = DemoPassword, firstName = "Kontrol", lastName = "Kullanıcısı" });
        if (!response.IsSuccessStatusCode)
            throw new BolumException($"Kayıt başarısız ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync()}");
        var body = await response.Content.ReadFromJsonAsync<JsonObject>(Json);
        return new ApiSession(this, body!["accessToken"]!.GetValue<string>());
    }

    public async Task<NpgsqlConnection> OpenDbAsync()
    {
        var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        return conn;
    }

    public async Task<T?> ScalarAsync<T>(string sql, params (string Name, object Value)[] parameters)
    {
        await using var conn = await OpenDbAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (name, value) in parameters) cmd.Parameters.AddWithValue(name, value);
        var result = await cmd.ExecuteScalarAsync();
        return result is null or DBNull ? default : (T)Convert.ChangeType(result, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task<int> ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var conn = await OpenDbAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (name, value) in parameters) cmd.Parameters.AddWithValue(name, value);
        return await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>Satışta, stoğu yeterli, kuruşu ",13" olmayan (sahte banka onu reddeder) bir varyant.</summary>
    public async Task<long> FindSellableVariantAsync(int minStock, decimal maxPrice = 400m)
    {
        var id = await ScalarAsync<long?>("""
            SELECT v.id FROM product_variants v
              JOIN products p ON p.id = v.product_id
              JOIN sellers s ON s.id = p.seller_id
             WHERE v.is_active AND p.status = 3 AND s.status = 2 AND v.price <= @max
               AND (v.price * 100) % 100 <> 13
               AND (SELECT count(*) FROM stock_items si WHERE si.variant_id = v.id) = 1
               AND (SELECT si.quantity - si.reserved FROM stock_items si WHERE si.variant_id = v.id) >= @min
             ORDER BY v.id LIMIT 1
            """, ("max", maxPrice), ("min", minStock));
        return id ?? throw new BolumException("Uygun ürün bulunamadı. Örnek veriyi yeniden üretin: DataGen --profile small --reset");
    }

    /// <summary>Kontroller için rastgele müşteri e-postaları (örnek veriden).</summary>
    public async Task<List<string>> CustomerEmailsAsync(int count)
    {
        await using var conn = await OpenDbAsync();
        await using var cmd = new NpgsqlCommand("""
            SELECT u.email FROM users u
             WHERE u.is_active AND EXISTS (SELECT 1 FROM user_roles ur JOIN roles r ON r.id = ur.role_id WHERE ur.user_id = u.id AND r.name = 'customer')
               AND NOT EXISTS (SELECT 1 FROM user_roles ur JOIN roles r ON r.id = ur.role_id WHERE ur.user_id = u.id AND r.name <> 'customer')
             ORDER BY u.id DESC LIMIT @n
            """, conn);
        cmd.Parameters.AddWithValue("n", count);
        var list = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) list.Add(reader.GetString(0));
        return list;
    }
}

/// <summary>Giriş yapılmış API oturumu.</summary>
public class ApiSession(CheckContext context, string token)
{
    public string Token { get; } = token;

    public HttpRequestMessage Request(HttpMethod method, string path, object? body = null, IDictionary<string, string>? headers = null)
    {
        var request = new HttpRequestMessage(method, $"{context.ApiUrl}{path}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);
        if (body is not null) request.Content = JsonContent.Create(body, options: CheckContext.Json);
        foreach (var (k, v) in headers ?? new Dictionary<string, string>()) request.Headers.TryAddWithoutValidation(k, v);
        return request;
    }

    public async Task<(int Status, JsonNode? Body, HttpResponseMessage Response)> SendAsync(HttpMethod method, string path, object? body = null, IDictionary<string, string>? headers = null)
    {
        using var request = Request(method, path, body, headers);
        var response = await context.Http.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        JsonNode? json = null;
        if (text.Length > 0 && text.TrimStart() is ['{', ..] or ['[', ..]) json = JsonNode.Parse(text);
        return ((int)response.StatusCode, json, response);
    }

    public Task<(int Status, JsonNode? Body, HttpResponseMessage Response)> GetAsync(string path) => SendAsync(HttpMethod.Get, path);
    public Task<(int Status, JsonNode? Body, HttpResponseMessage Response)> PostAsync(string path, object? body = null, IDictionary<string, string>? headers = null) => SendAsync(HttpMethod.Post, path, body ?? new { }, headers);
    public Task<(int Status, JsonNode? Body, HttpResponseMessage Response)> PutAsync(string path, object body) => SendAsync(HttpMethod.Put, path, body);
    public Task<(int Status, JsonNode? Body, HttpResponseMessage Response)> DeleteAsync(string path) => SendAsync(HttpMethod.Delete, path);

    /// <summary>Sepeti boşaltır, kuponu kaldırır ve verilen varyantı ekler.</summary>
    public async Task<bool> PrepareCartAsync(long variantId, int quantity)
    {
        var cart = await GetAsync("/api/cart");
        foreach (var line in cart.Body?["lines"]?.AsArray() ?? [])
            await DeleteAsync($"/api/cart/items/{line!["id"]}");
        await DeleteAsync("/api/cart/coupon");
        var added = await PostAsync("/api/cart/items", new { variantId, quantity });
        return added.Status == 200;
    }

    public async Task<long> EnsureAddressAsync()
    {
        var list = await GetAsync("/api/me/addresses");
        if (list.Body?.AsArray().FirstOrDefault() is { } first) return first["id"]!.GetValue<long>();
        var created = await PostAsync("/api/me/addresses", new
        {
            title = "Ev", fullName = "Kontrol Kullanıcısı", phone = "05321234567", city = "İstanbul", district = "Kadıköy",
            line = "Moda Caddesi No 10 Daire 3", postalCode = "34710",
        });
        return created.Body!["id"]!.GetValue<long>();
    }
}
