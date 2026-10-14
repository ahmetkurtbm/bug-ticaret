namespace Tezgah.Infrastructure.Security;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "tezgah";
    public string Audience { get; set; } = "tezgah-web";

    /// <summary>En az 32 karakter. Geliştirme değeri appsettings.Development.json'da.</summary>
    public string SigningKey { get; set; } = default!;

    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 14;
}
