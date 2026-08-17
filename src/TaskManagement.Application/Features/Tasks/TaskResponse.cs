using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Features.Tasks;

public record TaskResponse(
    string Id,
    string ProjectId,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskItemPriority Priority,
    DateTime? DueDate,
    TaskAssigneeDto? AssignedTo,
    string CreatedById,
    DateTime CreatedAt,
    DateTime UpdatedAt);
