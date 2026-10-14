using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Tezgah.Api.BackgroundJobs;
using Tezgah.Api.Infrastructure;
using Tezgah.Application;
using Tezgah.Infrastructure;
using Tezgah.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Şirketin sunucuları Türkçe kültürle çalışır (para/tarih biçimleri, hata mesajları). Bu yüzden kodda
// büyük/küçük harf dönüşümü ve sayı ayrıştırma her zaman kültürden bağımsız yapılmalı (ToUpperInvariant,
// CultureInfo.InvariantCulture). Yoksa "i" ve "ı" farkı ya da "0,01" ile "0.01" farkı hataya döner.
var culture = CultureInfo.GetCultureInfo(builder.Configuration["Localization:Culture"] ?? "tr-TR");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Tezgah.Application.Common.Interfaces.ICurrentUser, CurrentUser>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt ayarları eksik.");
if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
    throw new InvalidOperationException("Jwt:SigningKey en az 32 karakter olmalı.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            NameClaimType = "sub",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("web", policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    .WithExposedHeaders("Server-Timing")));

builder.Services.AddHealthChecks();
builder.Services.AddHostedService<ExpireUnpaidOrdersJob>();
builder.Services.AddHostedService<OutboxPublisherJob>();

var app = builder.Build();

// Container ortamında (docker compose) şema açılışta güncellenir. Yerelde: dotnet ef database update.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<Tezgah.Infrastructure.Persistence.TezgahDbContext>();
    await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.MigrateAsync(db.Database);
}

app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Her yanıtta bu istekte kaç SQL sorgusu çalıştığı: Server-Timing: db;dur=12.3;desc="4 sorgu"
    app.Use(async (context, next) =>
    {
        var counter = context.RequestServices.GetRequiredService<Tezgah.Infrastructure.Diagnostics.QueryCounter>();
        context.Response.OnStarting(() =>
        {
            // Başlık değeri kültürden bağımsız yazılmalı: tr-TR "12,3" üretir ve başlık bozulur.
            var ms = counter.Duration.TotalMilliseconds.ToString("0.0", CultureInfo.InvariantCulture);
            context.Response.Headers.Append("Server-Timing", $"db;dur={ms};desc=\"{counter.Count} sorgu\"");
            context.Response.Headers.Append("Timing-Allow-Origin", "*");
            return Task.CompletedTask;
        });
        await next();
    });
}

app.UseRouting();
app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

// Entegrasyon testleri için.
public partial class Program;
