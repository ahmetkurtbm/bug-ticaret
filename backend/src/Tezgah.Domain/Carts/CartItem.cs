using Tezgah.Domain.Common;
using Tezgah.Domain.Catalog;

namespace Tezgah.Domain.Carts;

public class CartItem : AuditableEntity
{
    public long CartId { get; set; }
    public long VariantId { get; set; }
    public ProductVariant Variant { get; set; } = default!;
    public int Quantity { get; set; }

    /// <summary>Sepete eklendiği andaki fiyat; ödeme sırasında güncel fiyatla karşılaştırılır.</summary>
    public decimal UnitPriceSnapshot { get; set; }
}
