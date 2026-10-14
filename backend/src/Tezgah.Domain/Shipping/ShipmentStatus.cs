namespace Tezgah.Domain.Shipping;

public enum ShipmentStatus
{
    Created = 1,
    PickedUp = 2,
    InTransit = 3,
    OutForDelivery = 4,
    Delivered = 5,
    DeliveryFailed = 6,
    Returned = 7,
}
