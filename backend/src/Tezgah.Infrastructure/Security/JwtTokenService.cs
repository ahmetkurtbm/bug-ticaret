using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tezgah.Application.Common.Interfaces;
using Tezgah.Domain.Identity;

namespace Tezgah.Infrastructure.Security;

public class JwtTokenService(IOptions<JwtOptions> options, TimeProvider clock) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public AccessToken CreateAccessToken(User user, IReadOnlyCollection<string> roles, long? sellerId)
    {
        var now = clock.GetUtcNow();
        var expires = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };
        claims.AddRange(roles.Select(r => new Claim("role", r)));
        if (sellerId is not null)
            claims.Add(new Claim("seller_id", sellerId.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public string CreateRefreshToken() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

    public string HashRefreshToken(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
