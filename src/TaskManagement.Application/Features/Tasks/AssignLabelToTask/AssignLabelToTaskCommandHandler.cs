using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Features.Tasks.AssignLabelToTask;

public class AssignLabelToTaskCommandHandler : IRequestHandler<AssignLabelToTaskCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IProjectAccessService _projectAccess;

    public AssignLabelToTaskCommandHandler(
        IApplicationDbContext context,
        IProjectAccessService projectAccess)
    {
        _context = context;
        _projectAccess = projectAccess;
    }

    public async Task<Unit> Handle(AssignLabelToTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _context.TaskItems
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task is null)
        {
            throw new NotFoundException("Task", request.TaskId);
        }

        if (!await _projectAccess.CanContributeAsync(task.ProjectId, cancellationToken))
        {
            throw new ForbiddenAccessException("Only project owners, admins and members can modify tasks.");
        }

        var label = await _context.Labels
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.LabelId, cancellationToken);

        if (label is null)
        {
            throw new NotFoundException("Label", request.LabelId);
        }

        if (label.ProjectId != task.ProjectId)
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(
                    nameof(request.LabelId),
                    "The label does not belong to the task's project.")
            });
        }

        var alreadyAssigned = await _context.TaskItemLabels
            .AnyAsync(
                til => til.TaskItemId == task.Id && til.LabelId == label.Id,
                cancellationToken);

        if (!alreadyAssigned)
        {
            _context.TaskItemLabels.Add(new TaskItemLabel
            {
                TaskItemId = task.Id,
                LabelId = label.Id
            });
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
