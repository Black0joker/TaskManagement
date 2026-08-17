using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Pagination;

namespace TaskManagement.Application.Features.Comments.GetTaskComments;

public class GetTaskCommentsQueryHandler : IRequestHandler<GetTaskCommentsQuery, PagedResult<CommentResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IProjectAccessService _projectAccess;

    public GetTaskCommentsQueryHandler(
        IApplicationDbContext context,
        IProjectAccessService projectAccess)
    {
        _context = context;
        _projectAccess = projectAccess;
    }

    public async Task<PagedResult<CommentResponse>> Handle(GetTaskCommentsQuery request, CancellationToken cancellationToken)
    {
        var task = await _context.TaskItems
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task is null)
        {
            throw new NotFoundException("Task", request.TaskId);
        }

        if (!await _projectAccess.CanReadAsync(task.ProjectId, cancellationToken))
        {
            throw new ForbiddenAccessException("You do not have access to this task.");
        }

        var query = _context.Comments
            .AsNoTracking()
            .Where(c => c.TaskItemId == task.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var page = request.Pagination.NormalizedPage;
        var pageSize = request.Pagination.NormalizedPageSize;

        var items = await query
            .OrderBy(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CommentResponse(
                c.Id,
                c.TaskItemId,
                c.AuthorId,
                c.Author.FirstName + " " + c.Author.LastName,
                c.Content,
                c.CreatedAt,
                c.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<CommentResponse>(items, page, pageSize, totalCount);
    }
}
