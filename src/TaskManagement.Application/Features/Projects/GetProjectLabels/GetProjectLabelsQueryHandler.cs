using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Common.Exceptions;

namespace TaskManagement.Application.Features.Projects.GetProjectLabels;

public class GetProjectLabelsQueryHandler : IRequestHandler<GetProjectLabelsQuery, IReadOnlyList<ProjectLabelSummary>>
{
    private readonly IApplicationDbContext _context;

    public GetProjectLabelsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProjectLabelSummary>> Handle(GetProjectLabelsQuery request, CancellationToken cancellationToken)
    {
        var projectExists = await _context.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.ProjectId, cancellationToken);

        if (!projectExists)
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
