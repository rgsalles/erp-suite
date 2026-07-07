namespace Erp.Api.Services;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "ErpSuite";
    public string Audience { get; set; } = "ErpSuite.Web";
    public string Secret { get; set; } = "change-this-development-secret-key-with-at-least-32-characters";
    public int ExpiresMinutes { get; set; } = 480;
}
