namespace Tezgah.Domain.Inventory;

public enum StockMovementReason
{
    InitialLoad = 1,
    Purchase = 2,
    Sale = 3,
    Return = 4,
    Adjustment = 5,
    Damage = 6,
}
