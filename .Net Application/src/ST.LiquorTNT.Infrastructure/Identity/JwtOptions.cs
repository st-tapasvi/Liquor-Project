namespace ST.LiquorTNT.Infrastructure.Identity;

/// <summary>The "Jwt" configuration section. Expiry is not here: it comes from SECURITY_CONFIG.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ST.LiquorTNT";
    public string Audience { get; set; } = "ST.LiquorTNT.Web";
    public string SigningKey { get; set; } = string.Empty;
}
