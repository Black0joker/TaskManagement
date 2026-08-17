using MediatR;

namespace TaskManagement.Application.Features.Projects.ListProjects;

public sealed record ListProjectsQuery : IRequest<IReadOnlyList<ProjectResponse>>;
