using MediatR;

namespace TaskManagement.Application.Features.Projects.GetProject;

public sealed record GetProjectQuery(string Id) : IRequest<ProjectResponse>;
