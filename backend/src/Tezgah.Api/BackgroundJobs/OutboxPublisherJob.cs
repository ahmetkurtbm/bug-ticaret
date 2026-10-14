using Tezgah.Infrastructure.Messaging;

namespace Tezgah.Api.BackgroundJobs;

/// <summary>Outbox'taki olayları saniyede bir RabbitMQ'ya gönderir.</summary>
public class OutboxPublisherJob(IServiceScopeFactory scopes, ILogger<OutboxPublisherJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        do
        {
            try
            {
                int sent;
                do
                {
                    await using var scope = scopes.CreateAsyncScope();
                    sent = await scope.ServiceProvider.GetRequiredService<OutboxDispatcher>().DispatchAsync(stoppingToken);
                }
                while (sent > 0 && !stoppingToken.IsCancellationRequested); // birikmiş olay varsa beklemeden devam
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Outbox yayını başarısız");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
