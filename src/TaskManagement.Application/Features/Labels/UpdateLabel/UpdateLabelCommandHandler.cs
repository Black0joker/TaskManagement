using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Features.Projects;

namespace TaskManagement.Application.Features.Labels.UpdateLabel;

public class UpdateLabelCommandHandler : IRequestHandler<UpdateLabelCommand, ProjectLabelSummary>
{
    private readonly IApplicationDbContext _context;
    private readonly IProjectAccessService _projectAccess;

    public UpdateLabelCommandHandler(
        IApplicationDbContext context,
        IProjectAccessService projectAccess)
    {
        _context = context;
        _projectAccess = projectAccess;
    }

    public async Task<ProjectLabelSummary> Handle(UpdateLabelCommand request, CancellationToken cancellationToken)
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

        var duplicate = await _context.Labels
            .AsNoTracking()
            .AnyAsync(
                l => l.ProjectId == label.ProjectId &&
                     l.Id != label.Id &&
                     l.Name == request.Name,
                cancellationToken);

        if (duplicate)
        {
            throw new ConflictException("A label with this name already exists in the project.");
        }

        label.Name = request.Name;
        label.Color = request.Color;
        await _context.SaveChangesAsync(cancellationToken);

        return new ProjectLabelSummary(label.Id, label.Name, label.Color);
    }
}
