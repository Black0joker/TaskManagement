using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Authentication;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Domain.Authorization;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Infrastructure.Projects;

public class ProjectAccessService : IProjectAccessService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ProjectAccessService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ProjectMemberRole?> GetRoleAsync(string projectId, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return null;
        }

        // System administrators have implicit Owner-level access to every project.
        if (_currentUserService.IsInRole(ApplicationRoles.Admin))
        {
            return ProjectMemberRole.Owner;
        }

        // Membership rows are the single source of truth. Project creation always
        // adds an Owner row, and legacy projects were backfilled by the seeder.
        return await _context.ProjectMembers
            .AsNoTracking()
            .Where(pm => pm.ProjectId == projectId && pm.UserId == userId)
            .Select(pm => (ProjectMemberRole?)pm.Role)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> CanReadAsync(string projectId, CancellationToken cancellationToken = default)
    {
        return await GetRoleAsync(projectId, cancellationToken) is not null;
    }

    public async Task<bool> CanContributeAsync(string projectId, CancellationToken cancellationToken = default)
    {
        var role = await GetRoleAsync(projectId, cancellationToken);
        return role is ProjectMemberRole.Owner or ProjectMemberRole.Admin or ProjectMemberRole.Member;
    }

    public async Task<bool> CanManageAsync(string projectId, CancellationToken cancellationToken = default)
    {
        var role = await GetRoleAsync(projectId, cancellationToken);
        return role is ProjectMemberRole.Owner or ProjectMemberRole.Admin;
    }
}
