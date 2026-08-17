using MediatR;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Features.Tasks.UpdateTask;

public sealed record UpdateTaskCommand(
    string Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskItemPriority Priority,
    string? AssignedToId,
    DateTime? DueDate) : IRequest<TaskResponse>;
