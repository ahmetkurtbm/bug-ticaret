namespace Tezgah.Contracts.Events;

/// <summary>Monolitin RabbitMQ'ya yayınladığı olaylar. Exchange: <see cref="EventTopology.Exchange"/> (topic), routing key = olay tipi.</summary>
public static class EventTopology
{
    public const string Exchange = "tezgah.events";
    public const string OrderPaid = "order.paid";
    public const string OrderShipped = "order.shipped";
    public const string SellerApproved = "seller.approved";
}

public record OrderLine(string ProductName, string? VariantLabel, int Quantity, decimal LineTotal);

public record OrderPaidEvent(Guid EventId, long OrderId, string OrderNumber, string CustomerEmail, string CustomerName,
    decimal GrandTotal, IReadOnlyList<OrderLine> Lines, DateTimeOffset OccurredAt);

public record OrderShippedEvent(Guid EventId, long OrderId, string OrderNumber, string CustomerEmail, string CustomerName,
    string SellerName, string Carrier, string TrackingNumber, DateTimeOffset OccurredAt);

public record SellerApprovedEvent(Guid EventId, long SellerId, string StoreName, string OwnerEmail, string OwnerName, DateTimeOffset OccurredAt);
