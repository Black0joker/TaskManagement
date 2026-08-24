using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using TaskManagement.Infrastructure.Authentication;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Tests.Unit;

public class RefreshTokenCleanupServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly User _user;

    public RefreshTokenCleanupServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"cleanup-{Guid.NewGuid():N}")
            .Options;

        _context = new AppDbContext(options);

        _user = new User
        {
            Id = "user-1",
            UserName = "cleanup@test.local",
            Email = "cleanup@test.local",
            FirstName = "Clean",
            LastName = "Up"
        };

        _context.Users.Add(_user);
        _context.SaveChangesAsync().GetAwaiter().GetResult();
    }

    private RefreshToken AddToken(DateTime createdAt, DateTime expiresAt, DateTime? revokedAt = null)
    {
        var token = new RefreshToken
        {
            Token = Guid.NewGuid().ToString("N"),
            UserId = _user.Id,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
            RevokedAt = revokedAt
        };

        _context.RefreshTokens.Add(token);
        _context.SaveChangesAsync().GetAwaiter().GetResult();
        return token;
    }

    [Fact]
    public async Task DeleteStaleTokensAsync_RemovesExpiredTokens()
    {
        var expired = AddToken(DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(-3));
        var active = AddToken(DateTime.UtcNow, DateTime.UtcNow.AddDays(7));

        var deleted = await RefreshTokenCleanupService.DeleteStaleTokensAsync(_context);

        Assert.Equal(1, deleted);
        Assert.False(await _context.RefreshTokens.AnyAsync(t => t.Id == expired.Id));
        Assert.True(await _context.RefreshTokens.AnyAsync(t => t.Id == active.Id));
    }

    [Fact]
    public async Task DeleteStaleTokensAsync_RemovesRevokedTokens()
    {
        var revoked = AddToken(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(6), revokedAt: DateTime.UtcNow);
        var active = AddToken(DateTime.UtcNow, DateTime.UtcNow.AddDays(7));

        var deleted = await RefreshTokenCleanupService.DeleteStaleTokensAsync(_context);

        Assert.Equal(1, deleted);
        Assert.False(await _context.RefreshTokens.AnyAsync(t => t.Id == revoked.Id));
        Assert.True(await _context.RefreshTokens.AnyAsync(t => t.Id == active.Id));
    }

    [Fact]
    public async Task DeleteStaleTokensAsync_KeepsActiveTokens()
    {
        AddToken(DateTime.UtcNow, DateTime.UtcNow.AddDays(7));
        AddToken(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddDays(3));

        var deleted = await RefreshTokenCleanupService.DeleteStaleTokensAsync(_context);

        Assert.Equal(0, deleted);
        Assert.Equal(2, await _context.RefreshTokens.CountAsync());
    }

    [Fact]
    public async Task DeleteStaleTokensAsync_ReturnsZero_WhenNoStaleTokens()
    {
        AddToken(DateTime.UtcNow, DateTime.UtcNow.AddDays(7));

        var deleted = await RefreshTokenCleanupService.DeleteStaleTokensAsync(_context);

        Assert.Equal(0, deleted);
    }

    [Fact]
    public async Task DeleteStaleTokensAsync_RemovesExpiredAndRevoked_KeepsActive()
    {
        var expired = AddToken(DateTime.UtcNow.AddDays(-20), DateTime.UtcNow.AddDays(-13));
        var revoked = AddToken(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(2), revokedAt: DateTime.UtcNow.AddDays(-4));
        var active = AddToken(DateTime.UtcNow, DateTime.UtcNow.AddDays(7));
        var active2 = AddToken(DateTime.UtcNow.AddMinutes(-30), DateTime.UtcNow.AddDays(1));

        var deleted = await RefreshTokenCleanupService.DeleteStaleTokensAsync(_context);

        Assert.Equal(2, deleted);
        Assert.Equal(2, await _context.RefreshTokens.CountAsync());
        Assert.True(await _context.RefreshTokens.AnyAsync(t => t.Id == active.Id));
        Assert.True(await _context.RefreshTokens.AnyAsync(t => t.Id == active2.Id));
    }

    [Fact]
    public async Task DeleteStaleTokensAsync_IsIdempotent()
    {
        AddToken(DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(-3));

        var first = await RefreshTokenCleanupService.DeleteStaleTokensAsync(_context);
        var second = await RefreshTokenCleanupService.DeleteStaleTokensAsync(_context);

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        Assert.Equal(0, await _context.RefreshTokens.CountAsync());
    }

    public void Dispose() => _context.Dispose();
}
