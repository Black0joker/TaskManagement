using MediatR;

namespace TaskManagement.Application.Features.Tasks.GetTask;

public sealed record GetTaskQuery(string Id) : IRequest<TaskDetailsResponse>;
