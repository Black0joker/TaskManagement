using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Exceptions;

namespace TaskManagement.Application.Features.Projects.GetProject;

public class GetProjectQueryHandler : IRequestHandler<GetProjectQuery, ProjectResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IProjectAccessService _projectAccess;

    public GetProjectQueryHandler(
        IApplicationDbContext context,
        IProjectAccessService projectAccess)
    {
        _context = context;
        _projectAccess = projectAccess;
    }

    public async Task<ProjectResponse> Handle(GetProjectQuery request, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .Where(p => p.Id == request.Id)
            .Select(p => new ProjectResponse(
                p.Id,
                p.Name,
                p.Description,
                p.CreatedById,
                p.CreatedAt,
                p.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (project is null)
        {
            throw new NotFoundException("Project", request.Id);
        }

        if (!await _projectAccess.CanReadAsync(request.Id, cancellationToken))
        {
            throw new ForbiddenAccessException("You do not have access to this project.");
        }

        return project;
    }
}
