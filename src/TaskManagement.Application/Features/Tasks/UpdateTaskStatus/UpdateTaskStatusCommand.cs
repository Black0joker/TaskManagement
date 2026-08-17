using MediatR;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Features.Tasks.UpdateTaskStatus;

public sealed record UpdateTaskStatusCommand(string Id, TaskItemStatus Status) : IRequest<TaskResponse>;
