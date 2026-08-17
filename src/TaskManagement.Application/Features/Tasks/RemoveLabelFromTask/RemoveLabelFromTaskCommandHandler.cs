using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Exceptions;

namespace TaskManagement.Application.Features.Tasks.RemoveLabelFromTask;

public class RemoveLabelFromTaskCommandHandler : IRequestHandler<RemoveLabelFromTaskCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IProjectAccessService _projectAccess;

    public RemoveLabelFromTaskCommandHandler(
        IApplicationDbContext context,
        IProjectAccessService projectAccess)
    {
        _context = context;
        _projectAccess = projectAccess;
    }

    public async Task<Unit> Handle(RemoveLabelFromTaskCommand request, CancellationToken cancellationToken)
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

        var assignment = await _context.TaskItemLabels
            .FirstOrDefaultAsync(
                til => til.TaskItemId == task.Id && til.LabelId == request.LabelId,
                cancellationToken);

        if (assignment is null)
        {
            throw new NotFoundException("Task label assignment", $"{request.TaskId}/{request.LabelId}");
        }

        _context.TaskItemLabels.Remove(assignment);
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
