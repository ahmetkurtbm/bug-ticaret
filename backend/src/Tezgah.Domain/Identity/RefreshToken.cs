using Tezgah.Domain.Common;

namespace Tezgah.Domain.Identity;

/// <summary>Tek kullanımlık yenileme token'ı. Değerin kendisi değil SHA-256 özeti saklanır.</summary>
public class RefreshToken : Entity
{
    public long UserId { get; set; }
    public string TokenHash { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;
}
