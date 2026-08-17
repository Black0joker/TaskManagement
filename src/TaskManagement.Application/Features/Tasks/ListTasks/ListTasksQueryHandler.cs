using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Authentication;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Domain.Authorization;

namespace TaskManagement.Application.Features.Tasks.ListTasks;

public class ListTasksQueryHandler : IRequestHandler<ListTasksQuery, IReadOnlyList<TaskResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IProjectAccessService _projectAccess;
    private readonly ICurrentUserService _currentUserService;

    public ListTasksQueryHandler(
        IApplicationDbContext context,
        IProjectAccessService projectAccess,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _projectAccess = projectAccess;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<TaskResponse>> Handle(ListTasksQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Array.Empty<TaskResponse>();
        }

        var isSystemAdmin = _currentUserService.IsInRole(ApplicationRoles.Admin);

        var query = _context.TaskItems.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.ProjectId))
        {
            var projectExists = await _context.Projects
                .AsNoTracking()
                .AnyAsync(p => p.Id == request.ProjectId, cancellationToken);

            if (!projectExists)
            {
                throw new NotFoundException("Project", request.ProjectId);
            }

            if (!await _projectAccess.CanReadAsync(request.ProjectId, cancellationToken))
            {
                throw new ForbiddenAccessException("You do not have access to this project.");
            }

            query = query.Where(t => t.ProjectId == request.ProjectId);
        }
        else if (!isSystemAdmin)
        {
            // Non-admins only see tasks from projects they are members of.
            query = query.Where(t =>
                t.Project.ProjectMembers.Any(pm => pm.UserId == userId));
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TaskResponse(
                t.Id,
                t.ProjectId,
                t.Title,
                t.Description,
                t.Status,
                t.Priority,
                t.DueDate,
                t.AssignedToId,
                t.CreatedById,
                t.CreatedAt,
                t.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}
