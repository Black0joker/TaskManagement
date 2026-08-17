using MediatR;

namespace TaskManagement.Application.Features.Tasks.UpdateTaskAssignee;

public sealed record UpdateTaskAssigneeCommand(string Id, string? UserId) : IRequest<TaskResponse>;
