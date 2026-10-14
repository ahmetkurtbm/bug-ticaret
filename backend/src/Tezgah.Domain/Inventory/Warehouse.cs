using Tezgah.Domain.Common;

namespace Tezgah.Domain.Inventory;

public class Warehouse : Entity
{
    public long SellerId { get; set; }
    public string Name { get; set; } = default!;
    public string City { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
