using MediatR;

namespace TaskManagement.Application.Features.Tasks.AssignLabelToTask;

public sealed record AssignLabelToTaskCommand(string TaskId, string LabelId) : IRequest<Unit>;
