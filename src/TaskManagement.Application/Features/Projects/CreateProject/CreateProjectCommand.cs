using MediatR;

namespace TaskManagement.Application.Features.Projects.CreateProject;

public sealed record CreateProjectCommand(string Name, string? Description) : IRequest<ProjectResponse>;
