using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Exceptions;

namespace TaskManagement.Application.Features.Tasks.GetTask;

public class GetTaskQueryHandler : IRequestHandler<GetTaskQuery, TaskDetailsResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IProjectAccessService _projectAccess;

    public GetTaskQueryHandler(
        IApplicationDbContext context,
        IProjectAccessService projectAccess)
    {
        _context = context;
        _projectAccess = projectAccess;
    }

    public async Task<TaskDetailsResponse> Handle(GetTaskQuery request, CancellationToken cancellationToken)
    {
        var task = await _context.TaskItems
            .AsNoTracking()
            .Where(t => t.Id == request.Id)
            .Select(t => new TaskDetailsResponse(
                t.Id,
                t.ProjectId,
                t.Title,
                t.Description,
                t.Status,
                t.Priority,
                t.DueDate,
                t.AssignedTo != null
                    ? new TaskAssigneeDto(t.AssignedTo.Id, t.AssignedTo.FirstName + " " + t.AssignedTo.LastName)
                    : null,
                t.CreatedById,
                t.CreatedAt,
                t.UpdatedAt,
                t.TaskItemLabels
                    .Select(til => new TaskLabelDto(til.Label.Id, til.Label.Name, til.Label.Color))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        if (task is null)
        {
            throw new NotFoundException("Task", request.Id);
        }

        if (!await _projectAccess.CanReadAsync(task.ProjectId, cancellationToken))
        {
            throw new ForbiddenAccessException("You do not have access to this task.");
        }

        return task;
    }
}
