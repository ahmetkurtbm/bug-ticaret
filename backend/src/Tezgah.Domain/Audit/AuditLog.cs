using Tezgah.Domain.Common;

namespace Tezgah.Domain.Audit;

public class AuditLog : Entity
{
    public long? UserId { get; set; }
    public string Action { get; set; } = default!;
    public string EntityType { get; set; } = default!;
    public long? EntityId { get; set; }

    /// <summary>Değişiklik ayrıntısı, JSON.</summary>
    public string? Data { get; set; }

    public string? IpAddress { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
