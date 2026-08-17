using MediatR;

namespace TaskManagement.Application.Features.Comments.CreateComment;

public sealed record CreateCommentCommand(
    string TaskId,
    string Content) : IRequest<CommentResponse>;
