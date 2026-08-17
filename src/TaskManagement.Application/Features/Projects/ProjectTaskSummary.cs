using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Features.Projects;

public sealed record ProjectTaskSummary(
    string Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskItemPriority Priority,
    DateTime? DueDate,
    string? AssignedToId,
    DateTime CreatedAt);
