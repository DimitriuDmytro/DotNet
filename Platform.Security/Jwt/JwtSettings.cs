namespace Platform.Security.Jwt;

/// <summary>
/// Налаштування JWT-токенів для Auth Service та мікросервісів платформи (Критерій 2).
/// </summary>
public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Issuer { get; set; } = "https://platform.auth";
    public string Audience { get; set; } = "platform.services";
    public string SecretKey { get; set; } = "SuperSecretKeyForDevelopmentTestingOnlyMustBeAtLeast32BytesLong!";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
