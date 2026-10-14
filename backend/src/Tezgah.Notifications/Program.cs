using Microsoft.EntityFrameworkCore;
using Tezgah.Notifications;
using Tezgah.Notifications.Api;
using Tezgah.Notifications.Email;
using Tezgah.Notifications.Messaging;
using Tezgah.Notifications.Persistence;

var builder = WebApplication.CreateBuilder(args);

var options = builder.Configuration.GetSection(NotificationsOptions.SectionName).Get<NotificationsOptions>()
    ?? throw new InvalidOperationException("Notifications ayarları eksik.");
if (string.IsNullOrWhiteSpace(options.ApiKey))
    throw new InvalidOperationException("Notifications:ApiKey ayarı eksik.");
var rabbit = builder.Configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
    ?? throw new InvalidOperationException("RabbitMq ayarları eksik.");
if (string.IsNullOrWhiteSpace(rabbit.HostName) || rabbit.Port <= 0)
    throw new InvalidOperationException("RabbitMq:HostName / Port ayarı eksik.");
if (string.IsNullOrWhiteSpace(rabbit.UserName) || string.IsNullOrWhiteSpace(rabbit.Password))
    throw new InvalidOperationException("RabbitMq:UserName / Password ayarı eksik.");
var smtp = builder.Configuration.GetSection(SmtpOptions.SectionName).Get<SmtpOptions>()
    ?? throw new InvalidOperationException("Smtp ayarları eksik.");
if (string.IsNullOrWhiteSpace(smtp.Host) || smtp.Port <= 0)
    throw new InvalidOperationException("Smtp:Host / Port ayarı eksik.");
if (string.IsNullOrWhiteSpace(smtp.From))
    throw new InvalidOperationException("Smtp:From ayarı eksik.");
var connectionString = builder.Configuration.GetConnectionString("Notifications");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:Notifications ayarı eksik.");

builder.Services.Configure<NotificationsOptions>(builder.Configuration.GetSection(NotificationsOptions.SectionName));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDbContext<NotificationsDbContext>(o => o
    .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history"))
    .UseSnakeCaseNamingConvention());

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<NotificationProcessor>();
builder.Services.AddHostedService<NotificationConsumer>();
builder.Services.AddHostedService<RetryWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Veritabanını da oluşturur.
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
}

// Middleware sırası önemlidir:
//  1. UseExceptionHandler en başta: sonrasındaki her şeyin (anahtar kontrolü dahil) fırlattığı hata ProblemDetails'e çevrilir.
//  2. UseRouting anahtar kontrolünden ÖNCE: ApiKeyMiddleware hangi uca anahtar gerektiğini endpoint metadata'sından
//     okur (GetEndpoint). Routing henüz çalışmadıysa endpoint null olur, kontrol atlanır ve korumalı uçlar anahtarsız açılır.
//  3. Anahtar kontrolü endpoint'lerden ÖNCE: ucun kodu çalıştıktan sonra reddetmek çok geçtir.
//  /health işaretsizdir, anahtar istemez.
app.UseExceptionHandler();
app.UseRouting();
app.UseMiddleware<ApiKeyMiddleware>();

app.MapHealthChecks("/health");
app.MapNotificationEndpoints();

app.Run();
