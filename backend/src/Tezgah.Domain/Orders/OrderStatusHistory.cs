using Tezgah.Domain.Common;

namespace Tezgah.Domain.Orders;

public class OrderStatusHistory : Entity
{
    public long OrderId { get; set; }
    public OrderStatus? FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public string? Note { get; set; }
    public long? ChangedByUserId { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}
