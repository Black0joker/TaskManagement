using MediatR;

namespace TaskManagement.Application.Features.Tasks.ListTasks;

public sealed record ListTasksQuery(
    string? ProjectId,
    bool Overdue = false,
    bool DueToday = false,
    bool DueThisWeek = false,
    bool NoDueDate = false,
    DateTime? DueBefore = null,
    DateTime? DueAfter = null) : IRequest<IReadOnlyList<TaskResponse>>;
