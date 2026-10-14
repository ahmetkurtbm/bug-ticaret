using Tezgah.Domain.Common;

namespace Tezgah.Domain.Catalog;

public class PriceHistory : Entity
{
    public long VariantId { get; set; }
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public long? ChangedByUserId { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}
