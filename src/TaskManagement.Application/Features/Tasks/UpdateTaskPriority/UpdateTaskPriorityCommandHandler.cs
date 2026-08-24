using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Features.Tasks.UpdateTaskPriority;

public class UpdateTaskPriorityCommandHandler : IRequestHandler<UpdateTaskPriorityCommand, TaskResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IProjectAccessService _projectAccess;

    public UpdateTaskPriorityCommandHandler(
        IApplicationDbContext context,
        IProjectAccessService projectAccess)
    {
        _context = context;
        _projectAccess = projectAccess;
    }

    public async Task<TaskResponse> Handle(UpdateTaskPriorityCommand request, CancellationToken cancellationToken)
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
                "Completed tasks are immutable. Priority cannot be modified.");
        }

        // Setting the same priority is a harmless no-op (idempotent PATCH).
        task.Priority = request.Priority!.Value;
        await _context.SaveChangesAsync(cancellationToken);

        return await TaskResponseFactory.CreateAsync(task, _context, cancellationToken);
    }
}
