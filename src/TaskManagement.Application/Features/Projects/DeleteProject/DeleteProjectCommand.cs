using MediatR;

namespace TaskManagement.Application.Features.Projects.DeleteProject;

public sealed record DeleteProjectCommand(string Id) : IRequest;
