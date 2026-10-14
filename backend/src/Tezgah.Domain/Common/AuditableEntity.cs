namespace Tezgah.Domain.Common;

/// <summary>Oluşturma ve güncelleme zamanı tutulan varlıklar. Zamanlar her zaman UTC.</summary>
public abstract class AuditableEntity : Entity
{
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
