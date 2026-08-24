using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TaskManagement.Application.Abstractions.Authentication;
using TaskManagement.Application.Abstractions.Identity;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.Common.Security;
using TaskManagement.Application.Features.Authentication.Login;
using TaskManagement.Tests.Unit.Testing;

namespace TaskManagement.Tests.Unit;

public class LoginCommandHandlerTests : HandlerTestBase
{
    private readonly StubIdentityService _identityService = new();
    private readonly StubJwtTokenGenerator _jwtTokenGenerator = new();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(
            _identityService,
            _jwtTokenGenerator,
            Context,
            Options.Create(new AuthenticationSettings()),
            NullLogger<LoginCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ReturnsTokens_WhenCredentialsAreValid()
    {
        var user = new ApplicationUserDto("user-1", "ada@test.local", "ada@test.local", "Ada", "Lovelace");
        _identityService.ValidationResult = CredentialValidationResult.Success(user);
        _identityService.RolesToReturn.Add("User");

        var response = await _handler.Handle(
            new LoginCommand("ada@test.local", "Str0ng!Pass"),
            CancellationToken.None);

        Assert.Equal("stub-access-token", response.AccessToken);
        Assert.Equal("Bearer", response.TokenType);
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
        Assert.Equal(_jwtTokenGenerator.LastExpiresAtUtc, response.AccessTokenExpiresAtUtc);

        // The JWT generator receives the user's roles for the token claims.
        Assert.Equal(new[] { "User" }, _jwtTokenGenerator.LastRoles);

        // The refresh token is stored hashed, never in plaintext.
        var stored = await Context.RefreshTokens.SingleAsync();
        Assert.Equal("user-1", stored.UserId);
        Assert.NotEqual(response.RefreshToken, stored.Token);
        Assert.Equal(RefreshTokenHasher.Hash(response.RefreshToken), stored.Token);
        Assert.True(stored.IsActive);
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorized_WhenCredentialsAreInvalid()
    {
        _identityService.ValidationResult = CredentialValidationResult.Invalid();

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => _handler.Handle(
            new LoginCommand("ada@test.local", "wrong-password"),
            CancellationToken.None));

        Assert.Equal("Invalid email or password.", ex.Message);
        Assert.False(await Context.RefreshTokens.AnyAsync());
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorized_WhenAccountIsLockedOut()
    {
        _identityService.ValidationResult = CredentialValidationResult.LockedOut();

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => _handler.Handle(
            new LoginCommand("ada@test.local", "Str0ng!Pass"),
            CancellationToken.None));

        Assert.Contains("locked", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(await Context.RefreshTokens.AnyAsync());
    }

    private sealed class StubIdentityService : IIdentityService
    {
        public CredentialValidationResult ValidationResult { get; set; } = CredentialValidationResult.Invalid();
        public List<string> RolesToReturn { get; } = new();

        public Task<ApplicationUserDto?> GetUserByEmailAsync(string email) =>
            Task.FromResult(ValidationResult.User);

        public Task<ApplicationUserDto?> GetUserByIdAsync(string userId) =>
            Task.FromResult(ValidationResult.User);

        public Task<IdentityOperationResult> CreateUserAsync(CreateApplicationUserRequest request, string password) =>
            Task.FromResult(IdentityOperationResult.Success("user-1"));

        public Task<CredentialValidationResult> ValidateCredentialsAsync(string email, string password) =>
            Task.FromResult(ValidationResult);

        public Task<IReadOnlyList<string>> GetRolesAsync(string userId) =>
            Task.FromResult<IReadOnlyList<string>>(RolesToReturn.ToList());
    }

    private sealed class StubJwtTokenGenerator : IJwtTokenGenerator
    {
        public string[] LastRoles { get; private set; } = [];
        public DateTime LastExpiresAtUtc { get; private set; }

        public (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(
            string userId,
            string email,
            IEnumerable<string> roles)
        {
            LastRoles = roles.ToArray();
            LastExpiresAtUtc = DateTime.UtcNow.AddMinutes(30);
            return ("stub-access-token", LastExpiresAtUtc);
        }
    }
}
