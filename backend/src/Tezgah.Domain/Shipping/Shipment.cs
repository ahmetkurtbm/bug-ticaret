using Tezgah.Domain.Common;

namespace Tezgah.Domain.Shipping;

public class Shipment : AuditableEntity
{
    public long OrderId { get; set; }
    public long SellerId { get; set; }
    public string Carrier { get; set; } = default!;
    public string? TrackingNumber { get; set; }
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Created;
    public DateTimeOffset? ShippedAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public List<ShipmentEvent> Events { get; set; } = new();
}
