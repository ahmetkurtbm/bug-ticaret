using Tezgah.Domain.Common;

namespace Tezgah.Domain.Shipping;

public class ShipmentEvent : Entity
{
    public long ShipmentId { get; set; }
    public ShipmentStatus Status { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
