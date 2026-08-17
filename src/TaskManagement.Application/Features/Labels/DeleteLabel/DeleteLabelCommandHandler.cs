using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Exceptions;

namespace TaskManagement.Application.Features.Labels.DeleteLabel;

public class DeleteLabelCommandHandler : IRequestHandler<DeleteLabelCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IProjectAccessService _projectAccess;

    public DeleteLabelCommandHandler(
        IApplicationDbContext context,
        IProjectAccessService projectAccess)
    {
        _context = context;
        _projectAccess = projectAccess;
    }

    public async Task<Unit> Handle(DeleteLabelCommand request, CancellationToken cancellationToken)
    {
        var label = await _context.Labels
            .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);

        if (label is null)
        {
            throw new NotFoundException("Label", request.Id);
        }

        if (!await _projectAccess.CanManageAsync(label.ProjectId, cancellationToken))
        {
            throw new ForbiddenAccessException("Only project owners and admins can manage labels.");
        }

        var assignments = await _context.TaskItemLabels
            .Where(til => til.LabelId == label.Id)
            .ToListAsync(cancellationToken);

        _context.TaskItemLabels.RemoveRange(assignments);
        _context.Labels.Remove(label);
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
