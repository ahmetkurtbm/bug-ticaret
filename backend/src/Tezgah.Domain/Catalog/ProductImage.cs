using Tezgah.Domain.Common;

namespace Tezgah.Domain.Catalog;

public class ProductImage : Entity
{
    public long ProductId { get; set; }
    public string Url { get; set; } = default!;
    public string? AltText { get; set; }
    public int SortOrder { get; set; }
}
