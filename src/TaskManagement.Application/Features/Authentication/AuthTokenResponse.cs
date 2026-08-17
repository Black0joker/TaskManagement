namespace TaskManagement.Application.Features.Authentication;

public sealed record AuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    string TokenType = "Bearer");
