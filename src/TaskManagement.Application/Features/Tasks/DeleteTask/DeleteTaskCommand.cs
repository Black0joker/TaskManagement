using MediatR;

namespace TaskManagement.Application.Features.Tasks.DeleteTask;

public sealed record DeleteTaskCommand(string Id) : IRequest;
