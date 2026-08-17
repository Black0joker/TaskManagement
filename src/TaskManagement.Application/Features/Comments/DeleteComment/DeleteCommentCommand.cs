using MediatR;

namespace TaskManagement.Application.Features.Comments.DeleteComment;

public sealed record DeleteCommentCommand(string Id) : IRequest<Unit>;
