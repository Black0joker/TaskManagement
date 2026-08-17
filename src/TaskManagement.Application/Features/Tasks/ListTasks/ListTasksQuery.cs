using MediatR;
using TaskManagement.Application.Common.Pagination;
using TaskManagement.Domain.Enums;

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
    int PageSize = 20,
    TaskItemStatus? Status = null,
    TaskItemPriority? Priority = null,
    string? AssignedToId = null,
    string? LabelId = null,
    DateTime? DueFrom = null,
    DateTime? DueTo = null,
    string? SortBy = null,
    string? SortDirection = null) : IRequest<PagedResult<TaskResponse>>;
