using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Rules;

namespace TaskManagement.Application.Features.Tasks.UpdateTask;

public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, TaskResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IProjectAccessService _projectAccess;

    public UpdateTaskCommandHandler(
        IApplicationDbContext context,
        IProjectAccessService projectAccess)
    {
        _context = context;
        _projectAccess = projectAccess;
    }

    public async Task<TaskResponse> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
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

        // Done tasks are immutable for status, priority, assignee and due date.
        if (task.Status == TaskItemStatus.Done)
        {
            var attemptsChange = request.Status != task.Status
                || request.Priority != task.Priority
                || !string.Equals(
                    string.IsNullOrWhiteSpace(request.AssignedToId) ? null : request.AssignedToId,
                    task.AssignedToId,
                    StringComparison.Ordinal)
                || request.DueDate != task.DueDate;

            if (attemptsChange)
            {
                throw new BusinessRuleException(
                    "Completed tasks are immutable. Status, priority, assignee and due date cannot be modified.");
            }
        }

        // Enforce status workflow governance when the status is being changed.
        if (request.Status != task.Status)
        {
            // Backward transitions (rework, reopen, resurrect, cancel completed
            // work) require project Owner/Admin.
            if (TaskStatusTransitions.IsBackward(task.Status, request.Status) &&
                !await _projectAccess.CanManageAsync(task.ProjectId, cancellationToken))
            {
                throw new ForbiddenAccessException(
                    "Only project owners and admins can move a task backwards.");
            }

            // Unassigned tasks cannot enter InProgress.
            var effectiveAssignee = string.IsNullOrWhiteSpace(request.AssignedToId)
                ? null
                : request.AssignedToId;

            if (request.Status == TaskItemStatus.InProgress &&
                effectiveAssignee is null &&
                task.AssignedToId is null)
            {
                throw new BusinessRuleException(
                    "A task must be assigned before it can be moved to InProgress.");
            }
        }

        // Due-date governance: active statuses require a due date, terminal
        // statuses forbid one.
        if (request.Status is TaskItemStatus.Todo or TaskItemStatus.InProgress or TaskItemStatus.InReview &&
            request.DueDate is null)
        {
            throw new BusinessRuleException(
                $"Tasks in '{request.Status}' status require a due date.");
        }

        if (request.Status is TaskItemStatus.Done or TaskItemStatus.Cancelled &&
            request.DueDate is not null)
        {
            throw new BusinessRuleException(
                $"Tasks in '{request.Status}' status cannot have a due date.");
        }

        var assignedToId = string.IsNullOrWhiteSpace(request.AssignedToId) ? null : request.AssignedToId;

        if (assignedToId is not null)
        {
            var assigneeIsMember = await _context.ProjectMembers
                .AnyAsync(
                    pm => pm.ProjectId == task.ProjectId && pm.UserId == assignedToId,
                    cancellationToken);

            if (!assigneeIsMember)
            {
                throw new ValidationException(new[]
                {
                    new FluentValidation.Results.ValidationFailure(
                        nameof(request.AssignedToId),
                        "The assigned user must be a member of the project.")
                });
            }
        }

        task.Title = request.Title;
        task.Description = request.Description;
        task.Status = request.Status;
        task.Priority = request.Priority;
        task.AssignedToId = assignedToId;
        task.DueDate = request.DueDate;

        await _context.SaveChangesAsync(cancellationToken);

        return await TaskResponseFactory.CreateAsync(task, _context, cancellationToken);
    }
}
