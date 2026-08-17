using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskManagement.Application.Abstractions.Authentication;
using TaskManagement.Application.Abstractions.Identity;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.Common.Security;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Features.Authentication.Refresh;

public class RefreshCommandHandler : IRequestHandler<RefreshCommand, AuthTokenResponse>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IApplicationDbContext _dbContext;
    private readonly AuthenticationSettings _authSettings;

    public RefreshCommandHandler(
        IIdentityService identityService,
        IJwtTokenGenerator jwtTokenGenerator,
        IApplicationDbContext dbContext,
        IOptions<AuthenticationSettings> authSettings)
    {
        _identityService = identityService;
        _jwtTokenGenerator = jwtTokenGenerator;
        _dbContext = dbContext;
        _authSettings = authSettings.Value;
    }

    public async Task<AuthTokenResponse> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        var hashedToken = RefreshTokenHasher.Hash(request.RefreshToken);

        var storedToken = await _dbContext.RefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.Token == hashedToken, cancellationToken);

        if (storedToken is null || !storedToken.IsActive)
        {
            throw new UnauthorizedException("Invalid or expired refresh token.");
        }

        var user = storedToken.User;
        var roles = await _identityService.GetRolesAsync(user.Id);
        var (accessToken, accessTokenExpiresAt) = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email!, roles);

        // Rotate: revoke the current token and issue a replacement.
        var newRefreshTokenValue = RefreshTokenHasher.Generate();
        var newRefreshToken = new RefreshToken
        {
            Token = RefreshTokenHasher.Hash(newRefreshTokenValue),
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_authSettings.RefreshTokenExpirationDays)
        };

        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByToken = newRefreshToken.Token;

        _dbContext.RefreshTokens.Add(newRefreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthTokenResponse(accessToken, newRefreshTokenValue, accessTokenExpiresAt);
    }
}
