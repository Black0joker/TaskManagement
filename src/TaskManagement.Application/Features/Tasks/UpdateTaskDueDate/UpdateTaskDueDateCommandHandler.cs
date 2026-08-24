using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Features.Tasks.UpdateTaskDueDate;

public class UpdateTaskDueDateCommandHandler : IRequestHandler<UpdateTaskDueDateCommand, TaskResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IProjectAccessService _projectAccess;

    public UpdateTaskDueDateCommandHandler(
        IApplicationDbContext context,
        IProjectAccessService projectAccess)
    {
        _context = context;
        _projectAccess = projectAccess;
    }

    public async Task<TaskResponse> Handle(UpdateTaskDueDateCommand request, CancellationToken cancellationToken)
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

        if (task.Status == TaskItemStatus.Done)
        {
            throw new BusinessRuleException(
                "Completed tasks are immutable. The due date cannot be modified.");
        }

        // Due-date governance: active statuses require a due date, terminal
        // statuses forbid one.
        if (task.Status is TaskItemStatus.Todo or TaskItemStatus.InProgress or TaskItemStatus.InReview &&
            request.DueDate is null)
        {
            throw new BusinessRuleException(
                $"Tasks in '{task.Status}' status require a due date.");
        }

        if (task.Status == TaskItemStatus.Cancelled && request.DueDate is not null)
        {
            throw new BusinessRuleException(
                $"Tasks in '{task.Status}' status cannot have a due date.");
        }

        task.DueDate = request.DueDate;
        await _context.SaveChangesAsync(cancellationToken);

        return await TaskResponseFactory.CreateAsync(task, _context, cancellationToken);
    }
}
