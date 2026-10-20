using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Tezgah.Contracts.Events;

namespace Tezgah.Notifications.Messaging;

/// <summary>
/// RabbitMQ kuyruğunu dinler. Teslimat en az bir kez: tekrarlar veritabanındaki event_id UNIQUE kısıtıyla elenir.
/// Bağlantı koparsa artan bekleme (en fazla 30 sn) ile yeniden bağlanır.
/// </summary>
public class NotificationConsumer(
    IOptions<RabbitMqOptions> options,
    NotificationProcessor processor,
    ILogger<NotificationConsumer> logger) : BackgroundService
{
    public const string Queue = "notifications.email";
    public const string DeadLetterQueue = "notifications.email.dlq";
    public const string DeadLetterExchange = "tezgah.events.dlx";
    private const ushort Prefetch = 10;
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var backoff = TimeSpan.FromSeconds(1);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunConnectionAsync(() => backoff = TimeSpan.FromSeconds(1), stoppingToken);
                if (!stoppingToken.IsCancellationRequested)
                    logger.LogWarning("RabbitMQ bağlantısı koptu, yeniden bağlanılacak.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "RabbitMQ bağlantı hatası, {Seconds} sn sonra yeniden denenecek.", backoff.TotalSeconds);
            }

            try
            {
                await Task.Delay(backoff, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            backoff = TimeSpan.FromSeconds(Math.Min(backoff.TotalSeconds * 2, MaxBackoff.TotalSeconds));
        }
    }

    /// <summary>Bağlanır, topolojiyi kurar, tüketir; bağlantı kopana ya da iptale kadar döner.</summary>
    private async Task RunConnectionAsync(Action onConnected, CancellationToken ct)
    {
        var o = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = o.HostName,
            Port = o.Port,
            UserName = o.UserName,
            Password = o.Password,
            // Yeniden bağlanmayı kendimiz yönetiyoruz.
            AutomaticRecoveryEnabled = false,
        };

        await using var connection = await factory.CreateConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.ConnectionShutdownAsync += (_, _) =>
        {
            closed.TrySetResult();
            return Task.CompletedTask;
        };

        await DeclareTopologyAsync(channel, ct);
        await channel.BasicQosAsync(0, Prefetch, false, ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, ea) => HandleAsync(channel, ea, ct);
        await channel.BasicConsumeAsync(Queue, autoAck: false, consumer, ct);

        logger.LogInformation("RabbitMQ'ya bağlanıldı, {Queue} kuyruğu dinleniyor.", Queue);
        onConnected();

        await closed.Task.WaitAsync(ct);
    }

    private static async Task DeclareTopologyAsync(IChannel channel, CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(EventTopology.Exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);
        await channel.ExchangeDeclareAsync(DeadLetterExchange, ExchangeType.Fanout, durable: true, autoDelete: false, cancellationToken: ct);

        await channel.QueueDeclareAsync(DeadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueBindAsync(DeadLetterQueue, DeadLetterExchange, "", cancellationToken: ct);

        await channel.QueueDeclareAsync(Queue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = DeadLetterExchange },
            cancellationToken: ct);
        foreach (var key in new[] { EventTopology.OrderPaid, EventTopology.OrderShipped, EventTopology.SellerApproved })
            await channel.QueueBindAsync(Queue, EventTopology.Exchange, key, cancellationToken: ct);
    }

    private async Task HandleAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken ct)
    {
        try
        {
            // Tip başlığı yoksa routing key olay tipiyle aynıdır.
            var type = ea.BasicProperties.Type ?? ea.RoutingKey;
            var result = await processor.ProcessAsync(type, ea.Body, ct);
            if (result == ProcessResult.Done)
                await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
            else
                await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Kapanış: ack verilmedi, mesaj yeniden teslim edilir.
        }
        catch (Exception ex) when (ex is not (Microsoft.EntityFrameworkCore.DbUpdateException or System.Data.Common.DbException or TimeoutException))
        {
            // Veriden kaynaklanan hata (ör. şablon beklemediği bir alanla karşılaştı): tekrar denemek düzeltmez, DLQ'ya gönder.
            logger.LogError(ex, "Mesaj işlenemedi, DLQ'ya gönderiliyor: {MessageId}", ea.BasicProperties.MessageId);
            try { await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, ct); }
            catch (Exception nackEx) { logger.LogWarning(nackEx, "Nack gönderilemedi."); }
        }
        catch (Exception ex)
        {
            // Veritabanı hatası: satır yazılmadı, mesajı kuyruğa geri koy (sıcak döngüyü önlemek için kısa bekle).
            logger.LogError(ex, "Mesaj işlenemedi, kuyruğa geri konuyor: {MessageId}", ea.BasicProperties.MessageId);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
                await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true, ct);
            }
            catch (Exception nackEx)
            {
                logger.LogWarning(nackEx, "Nack gönderilemedi; bağlantı yeniden kurulunca mesaj yeniden teslim edilecek.");
            }
        }
    }
}
