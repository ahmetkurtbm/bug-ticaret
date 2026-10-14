using Tezgah.Domain.Common;

namespace Tezgah.Domain.Carts;

public class Cart : AuditableEntity
{
    public long? UserId { get; set; }

    /// <summary>Giriş yapmamış ziyaretçinin tarayıcı kimliği.</summary>
    public Guid? AnonymousId { get; set; }

    public string? CouponCode { get; set; }
    public List<CartItem> Items { get; set; } = new();
}
