using MediatR;

namespace TaskManagement.Application.Features.Tasks.ListTasks;

public sealed record ListTasksQuery(string? ProjectId) : IRequest<IReadOnlyList<TaskResponse>>;
