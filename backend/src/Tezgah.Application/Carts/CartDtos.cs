using System.ComponentModel.DataAnnotations;

namespace Tezgah.Application.Carts;

public record CartOwner(long? UserId, Guid? AnonymousId);

public record CartLineDto(
    long Id,
    long VariantId,
    long ProductId,
    string ProductName,
    string ProductSlug,
    string? VariantLabel,
    string? ImageUrl,
    string SellerName,
    decimal UnitPrice,
    bool PriceChanged,
    int Quantity,
    int AvailableQuantity,
    decimal DiscountAmount,
    decimal LineTotal);

public record CartDto(
    IReadOnlyList<CartLineDto> Lines,
    int ItemCount,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal ShippingTotal,
    decimal VatTotal,
    decimal GrandTotal,
    string? CouponCode,
    string? CouponError,
    decimal FreeShippingRemaining);

public class AddCartItemRequest
{
    [Range(1, long.MaxValue)]
    public long VariantId { get; set; }

    [Range(1, 20)]
    public int Quantity { get; set; } = 1;
}

public class UpdateCartItemRequest
{
    [Range(1, 20)]
    public int Quantity { get; set; }
}

public class ApplyCouponRequest
{
    [Required, MaxLength(40)]
    public string CouponCode { get; set; } = default!;
}
