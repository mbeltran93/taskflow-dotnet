namespace TaskFlow.Infrastructure.Security;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "TaskFlow.Api";
    public string Audience { get; set; } = "TaskFlow.Api";
    public int ExpirationMinutes { get; set; } = 120;
}
