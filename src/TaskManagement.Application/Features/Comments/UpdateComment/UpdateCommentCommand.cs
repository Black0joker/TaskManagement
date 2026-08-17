using MediatR;

namespace TaskManagement.Application.Features.Comments.UpdateComment;

public sealed record UpdateCommentCommand(
    string Id,
    string Content) : IRequest<CommentResponse>;
