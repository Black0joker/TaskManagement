using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Common.Exceptions;

namespace TaskManagement.Application.Features.Tasks.DeleteTask;

public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteTaskCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        // Authorization (Tasks.Delete) is enforced by the controller policy.
        // Comments and label links cascade at the database level.
        var task = await _context.TaskItems
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (task is null)
        {
            throw new NotFoundException("Task", request.Id);
        }

        _context.TaskItems.Remove(task);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
