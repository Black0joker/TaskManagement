using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Features.Projects;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Features.Labels.CreateProjectLabel;

public class CreateProjectLabelCommandHandler : IRequestHandler<CreateProjectLabelCommand, ProjectLabelSummary>
{
    private readonly IApplicationDbContext _context;
    private readonly IProjectAccessService _projectAccess;

    public CreateProjectLabelCommandHandler(
        IApplicationDbContext context,
        IProjectAccessService projectAccess)
    {
        _context = context;
        _projectAccess = projectAccess;
    }

    public async Task<ProjectLabelSummary> Handle(CreateProjectLabelCommand request, CancellationToken cancellationToken)
    {
        var projectExists = await _context.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.ProjectId, cancellationToken);

        if (!projectExists)
        {
            throw new NotFoundException("Project", request.ProjectId);
        }

        if (!await _projectAccess.CanManageAsync(request.ProjectId, cancellationToken))
        {
            throw new ForbiddenAccessException("Only project owners and admins can manage labels.");
        }

        // Fast-path duplicate check. The authoritative guard is the unique
        // database index on (Name, ProjectId); the pre-check only improves
        // the common-case error response.
        var duplicate = await _context.Labels
            .AsNoTracking()
            .AnyAsync(
                l => l.ProjectId == request.ProjectId && l.Name == request.Name,
                cancellationToken);

        if (duplicate)
        {
            throw new ConflictException("A label with this name already exists in the project.");
        }

        var label = new Label
        {
            ProjectId = request.ProjectId,
            Name = request.Name,
            Color = request.Color
        };

        _context.Labels.Add(label);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Lost the race against a concurrent insert: the unique index on
            // (Name, ProjectId) rejected the duplicate. Surface a 409 instead
            // of a 500.
            throw new ConflictException("A label with this name already exists in the project.");
        }

        return new ProjectLabelSummary(label.Id, label.Name, label.Color);
    }
}
