using Tezgah.Domain.Common;

namespace Tezgah.Domain.Inventory;

public class StockItem : AuditableEntity
{
    public long VariantId { get; set; }
    public long WarehouseId { get; set; }
    public int Quantity { get; set; }

    /// <summary>Ödeme bekleyen siparişler için ayrılmış adet.</summary>
    public int Reserved { get; set; }

    public int Available => Quantity - Reserved;
}
