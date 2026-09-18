namespace AlmacenCloud.Infrastructure.Identity;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";
    public string Issuer { get; init; } = "AlmacenCloud";
    public string Audience { get; init; } = "AlmacenCloud.Web";
    public string Secret { get; init; } = string.Empty;
    public int ExpirationMinutes { get; init; } = 60;
}
