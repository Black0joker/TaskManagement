using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Exceptions;

namespace TaskManagement.Application.Features.Projects.GetProjectTasks;

public class GetProjectTasksQueryHandler : IRequestHandler<GetProjectTasksQuery, IReadOnlyList<ProjectTaskSummary>>
{
    private readonly IApplicationDbContext _context;
    private readonly IProjectAccessService _projectAccess;

    public GetProjectTasksQueryHandler(
        IApplicationDbContext context,
        IProjectAccessService projectAccess)
    {
        _context = context;
        _projectAccess = projectAccess;
    }

    public async Task<IReadOnlyList<ProjectTaskSummary>> Handle(GetProjectTasksQuery request, CancellationToken cancellationToken)
    {
        var projectExists = await _context.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.ProjectId, cancellationToken);

        // The same 404 is returned whether the project does not exist or the
        // user cannot read it, so a project ID's existence cannot be probed.
        if (!projectExists || !await _projectAccess.CanReadAsync(request.ProjectId, cancellationToken))
        {
            throw new NotFoundException("Project", request.ProjectId);
        }

        return await _context.TaskItems
            .AsNoTracking()
            .Where(t => t.ProjectId == request.ProjectId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new ProjectTaskSummary(
                t.Id,
                t.Title,
                t.Description,
                t.Status,
                t.Priority,
                t.DueDate,
                t.AssignedToId,
                t.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
