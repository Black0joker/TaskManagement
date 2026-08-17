using MediatR;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Features.Tasks.UpdateTaskPriority;

public sealed record UpdateTaskPriorityCommand(string Id, TaskItemPriority? Priority) : IRequest<TaskResponse>;
