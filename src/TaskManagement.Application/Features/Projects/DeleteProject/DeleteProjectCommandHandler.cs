using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Features.Projects.DeleteProject;

public class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IProjectAccessService _projectAccess;

    public DeleteProjectCommandHandler(
        IApplicationDbContext context,
        IProjectAccessService projectAccess)
    {
        _context = context;
        _projectAccess = projectAccess;
    }

    public async Task Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .Include(p => p.Labels)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (project is null)
        {
            throw new NotFoundException("Project", request.Id);
        }

        // Defense in depth: only the project Owner may delete a project.
        var role = await _projectAccess.GetRoleAsync(request.Id, cancellationToken);
        if (role != ProjectMemberRole.Owner)
        {
            throw new ForbiddenAccessException("Only the project owner can delete a project.");
        }

        // Labels are Restrict-delete relative to Project, so remove them explicitly.
        // Their TaskItemLabel associations cascade on delete.
        _context.Labels.RemoveRange(project.Labels);

        // Removing the project cascades to its tasks (and their comments and label links).
        _context.Projects.Remove(project);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
