using Tezgah.Domain.Identity;

namespace Tezgah.Application.Common.Interfaces;

public record AccessToken(string Token, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    AccessToken CreateAccessToken(User user, IReadOnlyCollection<string> roles, long? sellerId);

    /// <summary>Rastgele yenileme token'ı üretir; çağıran sadece özetini saklar.</summary>
    string CreateRefreshToken();

    string HashRefreshToken(string token);
}
