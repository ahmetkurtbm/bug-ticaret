using Tezgah.Domain.Common;

namespace Tezgah.Domain.Catalog;

public class Brand : Entity
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
