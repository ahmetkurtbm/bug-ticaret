using Tezgah.Domain.Common;

namespace Tezgah.Domain.Catalog;

/// <summary>Kategoriye bağlı özellik tanımı (ör. "Ekran boyutu").</summary>
public class ProductAttribute : Entity
{
    public long CategoryId { get; set; }
    public string Name { get; set; } = default!;
    public string? Unit { get; set; }
    public bool IsFilterable { get; set; }
}
