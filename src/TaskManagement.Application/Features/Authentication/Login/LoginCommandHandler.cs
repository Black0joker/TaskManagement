using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskManagement.Application.Abstractions.Authentication;
using TaskManagement.Application.Abstractions.Identity;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.Common.Security;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Features.Authentication.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthTokenResponse>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IApplicationDbContext _dbContext;
    private readonly AuthenticationSettings _authSettings;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IIdentityService identityService,
        IJwtTokenGenerator jwtTokenGenerator,
        IApplicationDbContext dbContext,
        IOptions<AuthenticationSettings> authSettings,
        ILogger<LoginCommandHandler> logger)
    {
        _identityService = identityService;
        _jwtTokenGenerator = jwtTokenGenerator;
        _dbContext = dbContext;
        _authSettings = authSettings.Value;
        _logger = logger;
    }

    public async Task<AuthTokenResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.ValidateCredentialsAsync(request.Email, request.Password);

        if (!result.Succeeded)
        {
            // Only the email is logged; passwords and tokens are never written to logs.
            _logger.LogWarning(
                "Failed login attempt for email {Email} (lockedOut: {IsLockedOut})",
                request.Email,
                result.IsLockedOut);

            throw new UnauthorizedException(result.IsLockedOut
                ? "The account is temporarily locked due to multiple failed login attempts. Please try again later."
                : "Invalid email or password.");
        }

        var user = result.User!;

        var roles = await _identityService.GetRolesAsync(user.Id);
        var (accessToken, accessTokenExpiresAt) = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, roles);

        var refreshTokenValue = RefreshTokenHasher.Generate();
        var refreshToken = new RefreshToken
        {
            Token = RefreshTokenHasher.Hash(refreshTokenValue),
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_authSettings.RefreshTokenExpirationDays)
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User logged in ({UserId})", user.Id);

        return new AuthTokenResponse(accessToken, refreshTokenValue, accessTokenExpiresAt);
    }
}
