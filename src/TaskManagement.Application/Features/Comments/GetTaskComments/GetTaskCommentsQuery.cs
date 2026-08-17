using MediatR;

namespace TaskManagement.Application.Features.Comments.GetTaskComments;

public sealed record GetTaskCommentsQuery(string TaskId) : IRequest<IReadOnlyList<CommentResponse>>;
