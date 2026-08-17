namespace TaskManagement.Application.Features.Projects;

public sealed record ProjectResponse(
    string Id,
    string Name,
    string? Description,
    string CreatedById,
    DateTime CreatedAt,
    DateTime UpdatedAt);
