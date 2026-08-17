using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskManagement.Application.Abstractions.Authentication;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Features.Comments.CreateComment;

public class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, CommentResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IProjectAccessService _projectAccess;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateCommentCommandHandler> _logger;

    public CreateCommentCommandHandler(
        IApplicationDbContext context,
        IProjectAccessService projectAccess,
        ICurrentUserService currentUserService,
        ILogger<CreateCommentCommandHandler> logger)
    {
        _context = context;
        _projectAccess = projectAccess;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<CommentResponse> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var task = await _context.TaskItems
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task is null)
        {
            throw new NotFoundException("Task", request.TaskId);
        }

        if (!await _projectAccess.CanContributeAsync(task.ProjectId, cancellationToken))
        {
            throw new ForbiddenAccessException("Only project owners, admins and members can comment on tasks.");
        }

        var comment = new Comment
        {
            TaskItemId = task.Id,
            AuthorId = _currentUserService.UserId,
            Content = request.Content
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Comment created ({CommentId}) on task {TaskId} by user {UserId}",
            comment.Id,
            comment.TaskItemId,
            _currentUserService.UserId);

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
