using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Exceptions;

namespace TaskManagement.Application.Features.Projects.GetProjectLabels;

public class GetProjectLabelsQueryHandler : IRequestHandler<GetProjectLabelsQuery, IReadOnlyList<ProjectLabelSummary>>
{
    private readonly IApplicationDbContext _context;
    private readonly IProjectAccessService _projectAccess;

    public GetProjectLabelsQueryHandler(
        IApplicationDbContext context,
        IProjectAccessService projectAccess)
    {
        _context = context;
        _projectAccess = projectAccess;
    }

    public async Task<IReadOnlyList<ProjectLabelSummary>> Handle(GetProjectLabelsQuery request, CancellationToken cancellationToken)
    {
        var projectExists = await _context.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.ProjectId, cancellationToken);

        // The same 404 is returned whether the project does not exist or the
        // user cannot read it, so a project ID's existence cannot be probed.
        if (!projectExists || !await _projectAccess.CanReadAsync(request.ProjectId, cancellationToken))
        {
            throw new NotFoundException("Project", request.ProjectId);
        }

        return await _context.Labels
            .AsNoTracking()
            .Where(l => l.ProjectId == request.ProjectId)
            .OrderBy(l => l.Name)
            .Select(l => new ProjectLabelSummary(l.Id, l.Name, l.Color))
            .ToListAsync(cancellationToken);
    }
}
