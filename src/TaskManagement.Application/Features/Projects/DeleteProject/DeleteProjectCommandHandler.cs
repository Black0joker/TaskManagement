using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Common.Exceptions;

namespace TaskManagement.Application.Features.Projects.DeleteProject;

public class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteProjectCommandHandler(IApplicationDbContext context)
    {
        _context = context;
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

        // Labels are Restrict-delete relative to Project, so remove them explicitly.
        // Their TaskItemLabel associations cascade on delete.
        _context.Labels.RemoveRange(project.Labels);

        // Removing the project cascades to its tasks (and their comments and label links).
        _context.Projects.Remove(project);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
