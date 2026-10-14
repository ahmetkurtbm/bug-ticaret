using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tezgah.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using Tezgah.Application.Orders;
using Tezgah.Application.Payments;
using Tezgah.Contracts.Payments;
using Tezgah.Infrastructure.Payments;
using Tezgah.Infrastructure.Persistence;
using Tezgah.Infrastructure.Security;

namespace Tezgah.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Tezgah")
            ?? throw new InvalidOperationException("ConnectionStrings:Tezgah ayarlanmamış.");

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<Tezgah.Infrastructure.Diagnostics.QueryCounter>();
        services.AddScoped<Tezgah.Infrastructure.Diagnostics.QueryCountingInterceptor>();
        services.AddDbContext<TezgahDbContext>((sp, options) => options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history"))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<Tezgah.Infrastructure.Diagnostics.QueryCountingInterceptor>()));
        services.AddScoped<ITezgahDbContext>(sp => sp.GetRequiredService<TezgahDbContext>());

        // Eski raporlama modülü EF'yi kullanmaz; kendi bağlantı havuzuyla Dapper çalıştırır.
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        services.AddSingleton(_ => new Npgsql.NpgsqlDataSourceBuilder(connectionString).Build());
        services.AddScoped<Tezgah.Application.Reporting.IReportingQueries, Tezgah.Infrastructure.Reporting.DapperReportingQueries>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddOptions<OrderOptions>()
            .Bind(configuration.GetSection(OrderOptions.SectionName))
            .Validate(o => Uri.TryCreate(o.StorefrontBaseUrl, UriKind.Absolute, out var u) && (u.Scheme == "http" || u.Scheme == "https"),
                "Orders:StorefrontBaseUrl mutlak bir http(s) adresi olmalı.")
            .Validate(o => o.PaymentTimeoutMinutes is >= 5 and <= 1440, "Orders:PaymentTimeoutMinutes 5 ile 1440 arasında olmalı.")
            .ValidateOnStart();
        services.AddOptions<PaymentsOptions>()
            .Bind(configuration.GetSection(PaymentsOptions.SectionName))
            .Validate(o => Uri.IsWellFormedUriString(o.BaseUrl, UriKind.Absolute), "Payments:BaseUrl geçerli bir adres olmalı.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey), "Payments:ApiKey ayarlanmamış.")
            .Validate(o => (o.WebhookSecret?.Length ?? 0) >= 32, "Payments:WebhookSecret en az 32 karakter olmalı.")
            .ValidateOnStart();
        services.AddHttpClient<IPaymentGateway, HttpPaymentGateway>((sp, http) =>
        {
            var o = sp.GetRequiredService<IOptions<PaymentsOptions>>().Value;
            http.BaseAddress = new Uri(o.BaseUrl.TrimEnd('/') + "/");
            http.Timeout = TimeSpan.FromSeconds(o.TimeoutSeconds);
            http.DefaultRequestHeaders.Add(PaymentWebhookSignature.ApiKeyHeader, o.ApiKey);
        });

        services.AddOptions<Tezgah.Infrastructure.Messaging.RabbitMqOptions>()
            .Bind(configuration.GetSection(Tezgah.Infrastructure.Messaging.RabbitMqOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.HostName), "RabbitMq:HostName ayarlanmamış.")
            .ValidateOnStart();
        services.AddSingleton<Tezgah.Infrastructure.Messaging.RabbitMqPublisher>();
        services.AddScoped<Tezgah.Infrastructure.Messaging.OutboxDispatcher>();

        return services;
    }
}
