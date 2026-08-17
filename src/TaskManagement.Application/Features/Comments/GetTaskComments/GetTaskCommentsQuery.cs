using MediatR;
using TaskManagement.Application.Common.Pagination;

namespace TaskManagement.Application.Features.Comments.GetTaskComments;

public sealed record GetTaskCommentsQuery(
    string TaskId,
    PaginationParameters Pagination) : IRequest<PagedResult<CommentResponse>>;
