using MediatR;

namespace TaskManagement.Application.Features.Projects.UpdateProject;

public sealed record UpdateProjectCommand(string Id, string Name, string? Description) : IRequest<ProjectResponse>;
