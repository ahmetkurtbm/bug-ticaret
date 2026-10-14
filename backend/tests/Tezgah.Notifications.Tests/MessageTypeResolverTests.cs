using Tezgah.Contracts.Events;
using Tezgah.Notifications.Logic;

namespace Tezgah.Notifications.Tests;

public class MessageTypeResolverTests
{
    [Theory]
    [InlineData("order.paid", typeof(OrderPaidEvent))]
    [InlineData("order.shipped", typeof(OrderShippedEvent))]
    [InlineData("seller.approved", typeof(SellerApprovedEvent))]
    public void Resolve_KnownType_ReturnsClrType(string eventType, Type expected) =>
        Assert.Equal(expected, MessageTypeResolver.Resolve(eventType));

    [Theory]
    [InlineData("order.cancelled")]
    [InlineData("ORDER.PAID")]
    [InlineData("")]
    [InlineData(null)]
    public void Resolve_UnknownType_ReturnsNull(string? eventType) =>
        Assert.Null(MessageTypeResolver.Resolve(eventType));
}
