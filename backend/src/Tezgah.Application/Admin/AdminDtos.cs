using System.ComponentModel.DataAnnotations;
using Tezgah.Domain.Campaigns;
using Tezgah.Domain.Catalog;
using Tezgah.Domain.Orders;
using Tezgah.Domain.Sellers;

namespace Tezgah.Application.Admin;

public record AdminSellerDto(long Id, string StoreName, string Slug, string LegalName, string TaxNumber, string City, SellerStatus Status,
    decimal CommissionRate, string OwnerEmail, string OwnerName, int ProductCount, DateTimeOffset CreatedAt, DateTimeOffset? ApprovedAt);

public class SellerDecisionRequest
{
    [MaxLength(500)]
    public string? Note { get; set; }
}

public class UpdateCommissionRequest
{
    [Range(typeof(decimal), "0", "50", ErrorMessage = "Komisyon %0 ile %50 arasında olmalı.", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal CommissionRate { get; set; }
}

public record AdminProductDto(long Id, string Name, string Slug, string? ImageUrl, string SellerName, string CategoryName, ProductStatus Status,
    int VariantCount, decimal MinPrice, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);

public class AdminOrderQuery
{
    public string? Q { get; set; }
    public OrderStatus? Status { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public record AdminOrderListItemDto(long Id, string OrderNumber, string CustomerEmail, string CustomerName, DateTimeOffset PlacedAt,
    OrderStatus Status, decimal GrandTotal, int ItemCount, int SellerCount, string? CouponCode);

public record AdminOrderItemDto(string ProductName, string Sku, string? VariantLabel, string SellerName, int Quantity, decimal UnitPrice,
    decimal DiscountAmount, decimal LineTotal, decimal CommissionRate);

public record AdminPaymentDto(string Provider, string? ProviderPaymentId, decimal Amount, int InstallmentCount, Domain.Payments.PaymentStatus Status,
    string? FailureReason, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt);

public record AdminShipmentDto(string SellerName, string Carrier, string? TrackingNumber, Domain.Shipping.ShipmentStatus Status, DateTimeOffset? ShippedAt);

public record AdminOrderDetailDto(long Id, string OrderNumber, string CustomerEmail, string CustomerName, OrderStatus Status,
    DateTimeOffset PlacedAt, DateTimeOffset? PaidAt, decimal Subtotal, decimal DiscountTotal, decimal ShippingTotal, decimal GrandTotal,
    string? CouponCode, string? ShippingCity, IReadOnlyList<AdminOrderItemDto> Items, IReadOnlyList<AdminPaymentDto> Payments,
    IReadOnlyList<AdminShipmentDto> Shipments, IReadOnlyList<AdminStatusEntryDto> History);

public record AdminStatusEntryDto(OrderStatus? From, OrderStatus To, string? Note, DateTimeOffset ChangedAt);

public record AdminCouponDto(long Id, string Code, string Description, CouponType Type, decimal Value, decimal? MaxDiscountAmount,
    decimal MinOrderAmount, int? MaxUses, int? MaxUsesPerUser, int UsedCount, string? SellerName, DateTimeOffset StartsAt, DateTimeOffset EndsAt,
    bool IsActive, string State);

public class CreateCouponRequest
{
    [Required, RegularExpression(@"^[A-Z0-9]{4,20}$", ErrorMessage = "Kod 4-20 karakter, büyük harf ve rakam olmalı.")]
    public string Code { get; set; } = default!;

    [Required, MaxLength(200)]
    public string Description { get; set; } = default!;

    public CouponType Type { get; set; }

    [Range(typeof(decimal), "0.01", "100000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Value { get; set; }

    public decimal? MaxDiscountAmount { get; set; }

    [Range(typeof(decimal), "0", "1000000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal MinOrderAmount { get; set; }

    [Range(1, 10_000_000)]
    public int? MaxUses { get; set; }

    [Range(1, 100)]
    public int? MaxUsesPerUser { get; set; }

    public long? SellerId { get; set; }

    /// <summary>Kampanyanın başladığı gün (İstanbul saatiyle 00:00).</summary>
    public DateOnly StartsOn { get; set; }

    /// <summary>Kampanyanın son günü (İstanbul saatiyle o günün sonuna kadar geçerli).</summary>
    public DateOnly EndsOn { get; set; }
}

public record CampaignHistoryEntryDto(DateTimeOffset At, string Action, string? CouponCode, string? ActorEmail, string? Data);
