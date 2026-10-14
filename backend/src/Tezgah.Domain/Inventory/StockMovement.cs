using Tezgah.Domain.Common;

namespace Tezgah.Domain.Inventory;

/// <summary>Stok hareket kaydı. Sadece eklenir; düzeltme ters kayıtla yapılır.</summary>
public class StockMovement : Entity
{
    public long StockItemId { get; set; }
    public int Delta { get; set; }
    public StockMovementReason Reason { get; set; }
    public string? ReferenceType { get; set; }
    public long? ReferenceId { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
