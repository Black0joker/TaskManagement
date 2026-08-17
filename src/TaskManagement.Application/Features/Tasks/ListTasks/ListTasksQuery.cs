using MediatR;
using TaskManagement.Application.Common.Pagination;

namespace TaskManagement.Application.Features.Tasks.ListTasks;

public sealed record ListTasksQuery(
    string? ProjectId,
    bool Overdue = false,
    bool DueToday = false,
    bool DueThisWeek = false,
    bool NoDueDate = false,
    DateTime? DueBefore = null,
    DateTime? DueAfter = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<TaskResponse>>;
