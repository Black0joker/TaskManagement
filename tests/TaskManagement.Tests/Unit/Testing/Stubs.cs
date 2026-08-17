using TaskManagement.Application.Abstractions.Authentication;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Tests.Unit.Testing;

/// <summary>
/// Configurable in-memory implementation of the current-user abstraction.
/// </summary>
public sealed class StubCurrentUserService : ICurrentUserService
{
    private readonly HashSet<string> _roles = new(StringComparer.OrdinalIgnoreCase);

    public string? UserId { get; set; }
    public string? Email { get; set; }
    public bool IsAuthenticated => UserId is not null;

    public void AddRole(string role) => _roles.Add(role);

    public bool IsInRole(string role) => _roles.Contains(role);
}

/// <summary>
/// Role-driven stub of project access for tests that control authorization
/// outcomes directly instead of seeding membership rows.
/// </summary>
public sealed class StubProjectAccessService : IProjectAccessService
{
    public ProjectMemberRole? Role { get; set; }

    public Task<ProjectMemberRole?> GetRoleAsync(string projectId, CancellationToken cancellationToken = default)
        => Task.FromResult(Role);

    public Task<bool> CanReadAsync(string projectId, CancellationToken cancellationToken = default)
        => Task.FromResult(Role is not null);

    public Task<bool> CanContributeAsync(string projectId, CancellationToken cancellationToken = default)
        => Task.FromResult(Role is ProjectMemberRole.Owner or ProjectMemberRole.Admin or ProjectMemberRole.Member);

    public Task<bool> CanManageAsync(string projectId, CancellationToken cancellationToken = default)
        => Task.FromResult(Role is ProjectMemberRole.Owner or ProjectMemberRole.Admin);
}
