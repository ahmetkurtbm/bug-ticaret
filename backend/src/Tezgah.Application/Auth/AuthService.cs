using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tezgah.Application.Common.Exceptions;
using Tezgah.Application.Common.Interfaces;
using Tezgah.Application.Common.Text;
using Tezgah.Domain.Common;
using Tezgah.Domain.Identity;

namespace Tezgah.Application.Auth;

public class AuthService(
    ITezgahDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokens,
    TimeProvider clock,
    ILogger<AuthService> logger) : IAuthService
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(14);

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var normalized = SlugHelper.NormalizeEmail(request.Email);
        if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalized, ct))
            throw new ConflictException("email_taken", "Bu e-posta adresiyle kayıtlı bir hesap var.");

        var customerRole = await db.Roles.SingleAsync(r => r.Name == Role.Customer, ct);
        var user = new User
        {
            Email = request.Email.Trim(),
            NormalizedEmail = normalized,
            PasswordHash = passwordHasher.Hash(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
        };
        user.Roles.Add(new UserRole { Role = customerRole });
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Yeni müşteri kaydı {UserId}", user.Id);
        return await IssueAsync(user, ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var normalized = SlugHelper.NormalizeEmail(request.Email);
        var user = await db.Users.SingleOrDefaultAsync(u => u.NormalizedEmail == normalized, ct);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new DomainException("invalid_credentials", "E-posta veya şifre hatalı.");
        if (!user.IsActive)
            throw new DomainException("user_inactive", "Hesabınız askıya alınmış.");

        user.LastLoginAt = clock.GetUtcNow();
        return await IssueAsync(user, ct);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var hash = tokens.HashRefreshToken(refreshToken);
        var stored = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, ct);
        var now = clock.GetUtcNow();

        if (stored is null || !stored.IsActive(now))
            throw new DomainException("invalid_refresh_token", "Oturumunuzun süresi doldu, lütfen tekrar giriş yapın.");

        var user = await db.Users.SingleAsync(u => u.Id == stored.UserId, ct);
        var response = await IssueAsync(user, ct, revoke: stored);
        return response;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct)
    {
        var hash = tokens.HashRefreshToken(refreshToken);
        var stored = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored is { RevokedAt: null })
        {
            stored.RevokedAt = clock.GetUtcNow();
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task<AuthResponse> IssueAsync(User user, CancellationToken ct, RefreshToken? revoke = null)
    {
        var roles = await db.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.Role.Name)
            .ToListAsync(ct);
        var sellerId = await db.Sellers
            .Where(s => s.OwnerUserId == user.Id)
            .Select(s => (long?)s.Id)
            .FirstOrDefaultAsync(ct);

        var access = tokens.CreateAccessToken(user, roles, sellerId);
        var refresh = tokens.CreateRefreshToken();
        var refreshHash = tokens.HashRefreshToken(refresh);
        var now = clock.GetUtcNow();

        if (revoke is not null)
        {
            revoke.RevokedAt = now;
            revoke.ReplacedByTokenHash = refreshHash;
        }
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            CreatedAt = now,
            ExpiresAt = now.Add(RefreshTokenLifetime),
        });
        await db.SaveChangesAsync(ct);

        return new AuthResponse(
            access.Token,
            access.ExpiresAt,
            refresh,
            new AuthUserDto(user.Id, user.Email, user.FirstName, user.LastName, roles, sellerId));
    }
}
