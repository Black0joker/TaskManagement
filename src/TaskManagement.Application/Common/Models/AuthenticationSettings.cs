namespace TaskManagement.Application.Common.Models;

public class AuthenticationSettings
{
    public const string SectionName = "JwtSettings";

    public int RefreshTokenExpirationDays { get; set; } = 7;

    /// <summary>
    /// How often the background cleanup job removes expired and revoked
    /// refresh tokens, in minutes. Values below 1 are treated as 1.
    /// </summary>
    public int TokenCleanupIntervalMinutes { get; set; } = 60;
}
