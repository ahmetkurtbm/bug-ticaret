using Tezgah.Domain.Common;

namespace Tezgah.Domain.Catalog;

public class Category : Entity
{
    public long? ParentId { get; set; }
    public Category? Parent { get; set; }
    public List<Category> Children { get; set; } = new();
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
