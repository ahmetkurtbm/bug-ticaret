using Tezgah.Domain.Common;
using Tezgah.Domain.Identity;

namespace Tezgah.Domain.Orders;

public class Order : AuditableEntity
{
    /// <summary>Müşteriye gösterilen numara (ör. TZ-2026-0012345).</summary>
    public string OrderNumber { get; set; } = default!;

    public long UserId { get; set; }
    public User User { get; set; } = default!;
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;

    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal ShippingTotal { get; set; }
    public decimal VatTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; } = "TRY";

    public string? CouponCode { get; set; }

    /// <summary>İstemcinin gönderdiği Idempotency-Key; aynı anahtarla ikinci sipariş oluşmaz.</summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>Eski sistemden taşınan siparişlerde boş olabilir.</summary>
    public OrderAddress? ShippingAddress { get; set; }

    public OrderAddress? BillingAddress { get; set; }

    public DateTimeOffset PlacedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }

    public List<OrderItem> Items { get; set; } = new();
    public List<OrderStatusHistory> StatusHistory { get; set; } = new();
}
