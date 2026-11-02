using Tezgah.Application.Orders;

namespace Tezgah.Api.BackgroundJobs;

/// <summary>Dakikada bir ödenmemiş siparişlerin süresini kontrol eder. Süre Orders:PaymentTimeoutMinutes ayarından gelir.</summary>
public class ExpireUnpaidOrdersJob(IServiceScopeFactory scopes, ILogger<ExpireUnpaidOrdersJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ExpireUnpaidOrders>().RunAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // Bir turdaki hata işi durdurmasın; bir sonraki turda tekrar denenir.
                logger.LogError(ex, "Ödenmemiş sipariş temizliği başarısız");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
