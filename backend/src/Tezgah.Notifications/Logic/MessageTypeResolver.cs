using Tezgah.Contracts.Events;

namespace Tezgah.Notifications.Logic;

public static class MessageTypeResolver
{
    /// <summary>Olay tipi (routing key ile aynı) için CLR tipini döner; bilinmeyen tip için null.</summary>
    public static Type? Resolve(string? eventType) => eventType switch
    {
        EventTopology.OrderPaid => typeof(OrderPaidEvent),
        EventTopology.OrderShipped => typeof(OrderShippedEvent),
        EventTopology.SellerApproved => typeof(SellerApprovedEvent),
        _ => null,
    };
}
