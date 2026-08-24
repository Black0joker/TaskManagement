using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Common.Models;

namespace TaskManagement.Infrastructure.Authentication;

/// <summary>
/// Periodically deletes refresh tokens that can never be accepted again:
/// expired tokens and revoked (rotated / logged-out) tokens. Without this job
/// the RefreshTokens table grows without bound because every login and every
/// rotation inserts a new row.
/// </summary>
public class RefreshTokenCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RefreshTokenCleanupService> _logger;
    private readonly TimeSpan _interval;

    public RefreshTokenCleanupService(
        IServiceScopeFactory scopeFactory,
        IOptions<AuthenticationSettings> authSettings,
        ILogger<RefreshTokenCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = TimeSpan.FromMinutes(Math.Max(1, authSettings.Value.TokenCleanupIntervalMinutes));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Refresh token cleanup started; interval: {IntervalMinutes} minute(s).",
            _interval.TotalMinutes);

        using var timer = new PeriodicTimer(_interval);

        try
        {
            // Run once immediately after startup, then on every interval tick.
            do
            {
                await RunCleanupAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown; nothing to do.
        }
    }

    private async Task RunCleanupAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var deleted = await DeleteStaleTokensAsync(context, stoppingToken);

            if (deleted > 0)
            {
                _logger.LogInformation("Refresh token cleanup removed {DeletedCount} stale token(s).", deleted);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Never crash the host because of cleanup; log and retry on the next tick.
            _logger.LogError(ex, "Refresh token cleanup failed.");
        }
    }

    /// <summary>
    /// Deletes all refresh tokens that can never authenticate again:
    /// revoked tokens (rotation or logout) and expired tokens.
    /// Returns the number of rows removed.
    /// </summary>
    public static async Task<int> DeleteStaleTokensAsync(
        IApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var staleTokens = await context.RefreshTokens
            .Where(t => t.RevokedAt != null || t.ExpiresAt < now)
            .ToListAsync(cancellationToken);

        if (staleTokens.Count == 0)
        {
            return 0;
        }

        context.RefreshTokens.RemoveRange(staleTokens);
        await context.SaveChangesAsync(cancellationToken);

        return staleTokens.Count;
    }
}
