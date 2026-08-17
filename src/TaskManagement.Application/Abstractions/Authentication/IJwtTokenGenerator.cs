namespace TaskManagement.Application.Abstractions.Authentication;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(
        string userId,
        string email,
        IEnumerable<string> roles);
}
