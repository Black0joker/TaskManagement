using MediatR;

namespace TaskManagement.Application.Features.Tasks.UpdateTaskDueDate;

public sealed record UpdateTaskDueDateCommand(string Id, DateTime? DueDate) : IRequest<TaskResponse>;
