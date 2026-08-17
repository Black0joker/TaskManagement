using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Authentication;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Exceptions;

namespace TaskManagement.Application.Features.Comments.UpdateComment;

public class UpdateCommentCommandHandler : IRequestHandler<UpdateCommentCommand, CommentResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IProjectAccessService _projectAccess;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCommentCommandHandler(
        IApplicationDbContext context,
        IProjectAccessService projectAccess,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _projectAccess = projectAccess;
        _currentUserService = currentUserService;
    }

    public async Task<CommentResponse> Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var comment = await _context.Comments
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (comment is null)
        {
            throw new NotFoundException("Comment", request.Id);
        }

        var task = await _context.TaskItems
            .AsNoTracking()
            .FirstAsync(t => t.Id == comment.TaskItemId, cancellationToken);

        var isAuthor = comment.AuthorId == _currentUserService.UserId;
        var isAdmin = await _projectAccess.CanManageAsync(task.ProjectId, cancellationToken);

        if (!isAuthor && !isAdmin)
        {
            throw new ForbiddenAccessException("Only the comment author or project owners/admins can edit a comment.");
        }

        comment.Content = request.Content;
        await _context.SaveChangesAsync(cancellationToken);

        var authorName = await _context.Comments
            .AsNoTracking()
            .Where(c => c.Id == comment.Id)
            .Select(c => c.Author.FirstName + " " + c.Author.LastName)
            .FirstAsync(cancellationToken);

        return new CommentResponse(
            comment.Id,
            comment.TaskItemId,
            comment.AuthorId,
            authorName,
            comment.Content,
            comment.CreatedAt,
            comment.UpdatedAt);
    }
}
