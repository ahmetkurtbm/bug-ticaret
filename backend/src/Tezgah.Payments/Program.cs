using Microsoft.EntityFrameworkCore;
using Tezgah.Payments;
using Tezgah.Payments.Checkout;
using Tezgah.Payments.Controllers;
using Tezgah.Payments.Persistence;
using Tezgah.Payments.Webhooks;

var builder = WebApplication.CreateBuilder(args);

var options = builder.Configuration.GetSection(PaymentsOptions.SectionName).Get<PaymentsOptions>()
    ?? throw new InvalidOperationException("Payments ayarları eksik.");
if (string.IsNullOrWhiteSpace(options.ApiKey))
    throw new InvalidOperationException("Payments:ApiKey ayarı eksik.");
if (string.IsNullOrWhiteSpace(options.WebhookSecret))
    throw new InvalidOperationException("Payments:WebhookSecret ayarı eksik.");
if (string.IsNullOrWhiteSpace(options.WebhookUrl))
    throw new InvalidOperationException("Payments:WebhookUrl ayarı eksik.");
var connectionString = builder.Configuration.GetConnectionString("Payments");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:Payments ayarı eksik.");

builder.Services.Configure<PaymentsOptions>(builder.Configuration.GetSection(PaymentsOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDbContext<PaymentsDbContext>(o => o
    .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history"))
    .UseSnakeCaseNamingConvention());

// Views gerekli: [ValidateAntiForgeryToken] filtresi AddControllers ile kaydedilmiyor.
builder.Services.AddControllersWithViews();
// 3D formu için CSRF koruması. Birden fazla örnek çalışırsa Data Protection anahtarları paylaşılmalı.
builder.Services.AddAntiforgery();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<ApiKeyFilter>();
builder.Services.AddHealthChecks();

builder.Services.AddHttpClient(WebhookDeliveryWorker.HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddHostedService<WebhookDeliveryWorker>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    // Veritabanını da oluşturur.
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<PaymentsDbContext>().Database.MigrateAsync();
}

app.UseRouting();
app.MapHealthChecks("/health");
app.MapControllers();

// "Son ödemeler" paneli (sahte banka). API anahtarı gerekmez; müşteri e-postası seçilmez.
app.MapGet("/", async (PaymentsDbContext db, CancellationToken ct) =>
{
    var payments = await db.Payments.AsNoTracking()
        .OrderByDescending(p => p.CreatedAt)
        .Take(50)
        .Select(p => new { p.Id, p.CreatedAt, p.OrderNumber, p.Amount, p.InstallmentCount, p.Status, p.FailureReason })
        .ToListAsync(ct);

    var ids = payments.Select(p => p.Id).ToList();
    var deliveries = await db.WebhookDeliveries.AsNoTracking()
        .Where(d => ids.Contains(d.PaymentId))
        .Select(d => new { d.PaymentId, d.CreatedAt, Info = new PanelDelivery(d.DeliveredAt, d.AttemptCount, d.LastError) })
        .ToListAsync(ct);
    var latest = deliveries
        .GroupBy(d => d.PaymentId)
        .ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.CreatedAt).First().Info);

    var rows = payments
        .Select(p => new PanelRow(p.Id, p.CreatedAt, p.OrderNumber, p.Amount, p.InstallmentCount, p.Status,
            p.FailureReason, latest.GetValueOrDefault(p.Id)))
        .ToList();
    return Results.Content(PaymentsPanelPage.Render(rows), "text/html; charset=utf-8");
});

app.Run();

// Entegrasyon testleri için.
public partial class Program;
