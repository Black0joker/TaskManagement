using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Exceptions;

namespace TaskManagement.Application.Features.Projects.UpdateProject;

public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, ProjectResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IProjectAccessService _projectAccess;

    public UpdateProjectCommandHandler(
        IApplicationDbContext context,
        IProjectAccessService projectAccess)
    {
        _context = context;
        _projectAccess = projectAccess;
    }

    public async Task<ProjectResponse> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (project is null)
        {
            throw new NotFoundException("Project", request.Id);
        }

        if (!await _projectAccess.CanManageAsync(request.Id, cancellationToken))
        {
            throw new ForbiddenAccessException("Only project owners and admins can modify project settings.");
        }

        project.Name = request.Name;
        project.Description = request.Description;

        await _context.SaveChangesAsync(cancellationToken);

        return new ProjectResponse(
            project.Id,
            project.Name,
            project.Description,
            project.CreatedById,
            project.CreatedAt,
            project.UpdatedAt);
    }
}
