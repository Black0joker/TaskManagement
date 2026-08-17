using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Abstractions.Projects;

/// <summary>
/// Determines the current user's membership role within a project.
/// Used to enforce that users can only access projects they belong to.
/// </summary>
public interface IProjectAccessService
{
    /// <summary>
    /// Returns the current user's role in the project, or null when the user is not a member.
    /// The project creator is always treated as at least an Owner.
    /// </summary>
    Task<ProjectMemberRole?> GetRoleAsync(string projectId, CancellationToken cancellationToken = default);

    /// <summary>True when the current user is a member of (or created) the project.</summary>
    Task<bool> CanReadAsync(string projectId, CancellationToken cancellationToken = default);

    /// <summary>True when the current user can manage project content (Owner/Admin/Member).</summary>
    Task<bool> CanContributeAsync(string projectId, CancellationToken cancellationToken = default);

    /// <summary>True when the current user can manage members and settings (Owner/Admin).</summary>
    Task<bool> CanManageAsync(string projectId, CancellationToken cancellationToken = default);
}
