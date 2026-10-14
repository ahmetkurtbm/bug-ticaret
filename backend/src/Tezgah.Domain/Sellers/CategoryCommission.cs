using Tezgah.Domain.Common;

namespace Tezgah.Domain.Sellers;

/// <summary>Satıcıya ve kategoriye özel komisyon oranı.</summary>
public class CategoryCommission : Entity
{
    public long SellerId { get; set; }
    public long CategoryId { get; set; }
    public decimal Rate { get; set; }
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidUntil { get; set; }
}
