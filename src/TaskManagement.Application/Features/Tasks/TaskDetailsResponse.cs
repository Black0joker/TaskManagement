using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Features.Tasks;

public sealed record TaskDetailsResponse(
    string Id,
    string ProjectId,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskItemPriority Priority,
    DateTime? DueDate,
    string? AssignedToId,
    string CreatedById,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<TaskLabelDto> Labels);

public sealed record TaskLabelDto(string Id, string Name, string Color);
