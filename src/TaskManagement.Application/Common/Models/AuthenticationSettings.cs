namespace TaskManagement.Application.Common.Models;

public class AuthenticationSettings
{
    public const string SectionName = "JwtSettings";

    public int RefreshTokenExpirationDays { get; set; } = 7;
}
