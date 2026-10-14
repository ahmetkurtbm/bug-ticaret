using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Tezgah.Contracts.Events;

namespace Tezgah.Infrastructure.Messaging;

/// <summary>
/// Tek bağlantı, tek kanal; yayıncı onayı (publisher confirm) açık: BasicPublishAsync, broker mesajı
/// diske yazdığını onaylayana kadar bekler, reddederse hata fırlatır. Thread-safe değildir; tek yayıncı işi kullanır.
/// </summary>
public sealed class RabbitMqPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqPublisher> logger) : IAsyncDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync(Guid messageId, string type, ReadOnlyMemory<byte> body, DateTimeOffset occurredAt, CancellationToken ct)
    {
        var channel = await GetChannelAsync(ct);
        var props = new BasicProperties
        {
            MessageId = messageId.ToString(),
            Type = type,
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Timestamp = new AmqpTimestamp(occurredAt.ToUnixTimeSeconds()),
        };
        await channel.BasicPublishAsync(EventTopology.Exchange, type, mandatory: false, basicProperties: props, body: body, cancellationToken: ct);
    }

    /// <summary>Hata sonrası bağlantıyı bırakır; sonraki çağrı yeniden kurar.</summary>
    public async Task ResetAsync()
    {
        await DisposeAsync();
        _channel = null;
        _connection = null;
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true }) return _channel;
        await ResetAsync();

        var o = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = o.HostName,
            Port = o.Port,
            UserName = o.UserName,
            Password = o.Password,
            VirtualHost = o.VirtualHost,
            ClientProvidedName = "tezgah-api-outbox",
        };
        _connection = await factory.CreateConnectionAsync(ct);
        _channel = await _connection.CreateChannelAsync(new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), ct);
        await _channel.ExchangeDeclareAsync(EventTopology.Exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);
        logger.LogInformation("RabbitMQ bağlantısı kuruldu {Host}:{Port}", o.HostName, o.Port);
        return _channel;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_channel is not null) await _channel.DisposeAsync();
            if (_connection is not null) await _connection.DisposeAsync();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "RabbitMQ bağlantısı kapatılırken hata");
        }
    }
}
