using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Exceptions;

namespace TaskManagement.Application.Features.Tasks.UpdateTaskAssignee;

public class UpdateTaskAssigneeCommandHandler : IRequestHandler<UpdateTaskAssigneeCommand, TaskResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IProjectAccessService _projectAccess;

    public UpdateTaskAssigneeCommandHandler(
        IApplicationDbContext context,
        IProjectAccessService projectAccess)
    {
        _context = context;
        _projectAccess = projectAccess;
    }

    public async Task<TaskResponse> Handle(UpdateTaskAssigneeCommand request, CancellationToken cancellationToken)
    {
        var task = await _context.TaskItems
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (task is null)
        {
            throw new NotFoundException("Task", request.Id);
        }

        if (!await _projectAccess.CanContributeAsync(task.ProjectId, cancellationToken))
        {
            throw new ForbiddenAccessException("Only project owners, admins and members can modify tasks.");
        }

        var userId = string.IsNullOrWhiteSpace(request.UserId) ? null : request.UserId;

        if (userId is not null)
        {
            var assigneeIsMember = await _context.ProjectMembers
                .AnyAsync(
                    pm => pm.ProjectId == task.ProjectId && pm.UserId == userId,
                    cancellationToken);

            if (!assigneeIsMember)
            {
                throw new ValidationException(new[]
                {
                    new FluentValidation.Results.ValidationFailure(
                        nameof(request.UserId),
                        "The assigned user must be a member of the project.")
                });
            }
        }

        task.AssignedToId = userId;
        await _context.SaveChangesAsync(cancellationToken);

        return new TaskResponse(
            task.Id,
            task.ProjectId,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.DueDate,
            task.AssignedToId,
            task.CreatedById,
            task.CreatedAt,
            task.UpdatedAt);
    }
}
