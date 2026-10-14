using Tezgah.Domain.Common;

namespace Tezgah.Domain.Catalog;

public class ProductAttributeValue : Entity
{
    public long ProductId { get; set; }
    public long AttributeId { get; set; }
    public ProductAttribute Attribute { get; set; } = default!;
    public string Value { get; set; } = default!;
}
