using MediatR;

namespace TaskManagement.Application.Features.Tasks.RemoveLabelFromTask;

public sealed record RemoveLabelFromTaskCommand(string TaskId, string LabelId) : IRequest<Unit>;
