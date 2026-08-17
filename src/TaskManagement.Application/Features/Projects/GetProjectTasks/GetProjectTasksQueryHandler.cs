using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Common.Exceptions;

namespace TaskManagement.Application.Features.Projects.GetProjectTasks;

public class GetProjectTasksQueryHandler : IRequestHandler<GetProjectTasksQuery, IReadOnlyList<ProjectTaskSummary>>
{
    private readonly IApplicationDbContext _context;

    public GetProjectTasksQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProjectTaskSummary>> Handle(GetProjectTasksQuery request, CancellationToken cancellationToken)
    {
        var projectExists = await _context.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.ProjectId, cancellationToken);

        if (!projectExists)
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
