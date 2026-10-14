using System.ComponentModel.DataAnnotations;

namespace Tezgah.Application.Auth;

public class RegisterRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = default!;

    [Required, MinLength(8), MaxLength(128)]
    public string Password { get; set; } = default!;

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = default!;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = default!;
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    public string Password { get; set; } = default!;
}

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = default!;
}

public record AuthUserDto(long Id, string Email, string FirstName, string LastName, IReadOnlyList<string> Roles, long? SellerId);

public record AuthResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken, AuthUserDto User);
