using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Domain.Rules;

namespace TaskManagement.Application.Features.Tasks.UpdateTaskStatus;

public class UpdateTaskStatusCommandHandler : IRequestHandler<UpdateTaskStatusCommand, TaskResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IProjectAccessService _projectAccess;

    public UpdateTaskStatusCommandHandler(
        IApplicationDbContext context,
        IProjectAccessService projectAccess)
    {
        _context = context;
        _projectAccess = projectAccess;
    }

    public async Task<TaskResponse> Handle(UpdateTaskStatusCommand request, CancellationToken cancellationToken)
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

        if (TaskStatusTransitions.IsSame(task.Status, request.Status))
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(
                    nameof(request.Status),
                    $"The task is already in status '{request.Status}'.")
            });
        }

        // Moving work backwards (rework, reopening, resurrecting a cancelled
        // task, or cancelling completed work) is a governance action.
        if (TaskStatusTransitions.IsBackward(task.Status, request.Status) &&
            !await _projectAccess.CanManageAsync(task.ProjectId, cancellationToken))
        {
            throw new ForbiddenAccessException("Only project owners and admins can move a task backwards.");
        }

        task.Status = request.Status;
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
