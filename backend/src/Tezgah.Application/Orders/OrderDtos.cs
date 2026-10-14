using System.ComponentModel.DataAnnotations;
using Tezgah.Domain.Orders;

namespace Tezgah.Application.Orders;

public class PlaceOrderRequest
{
    [Range(1, long.MaxValue)]
    public long AddressId { get; set; }

    [AllowedValues(1, 2, 3, 6, 9, 12)]
    public int InstallmentCount { get; set; } = 1;
}

public record PlaceOrderResponse(string OrderNumber, string? CheckoutUrl, string? PaymentError);

public record OrderSummaryDto(
    string OrderNumber,
    OrderStatus Status,
    DateTimeOffset PlacedAt,
    decimal GrandTotal,
    int ItemCount,
    IReadOnlyList<string> ImageUrls);

public record OrderItemDto(
    long VariantId,
    string ProductName,
    string? ProductSlug,
    string? VariantLabel,
    string? ImageUrl,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal LineTotal);

public record OrderStatusEntryDto(OrderStatus Status, DateTimeOffset ChangedAt);

public record OrderAddressDto(string FullName, string? Phone, string City, string District, string Line, string? PostalCode);

public record OrderDetailDto(
    string OrderNumber,
    OrderStatus Status,
    DateTimeOffset PlacedAt,
    DateTimeOffset? PaidAt,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal ShippingTotal,
    decimal VatTotal,
    decimal GrandTotal,
    string? CouponCode,
    OrderAddressDto? ShippingAddress,
    IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<OrderStatusEntryDto> History,
    bool CanRetryPayment,
    bool LastPaymentFailed);
