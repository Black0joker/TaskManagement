using MediatR;

namespace TaskManagement.Application.Features.Labels.DeleteLabel;

public sealed record DeleteLabelCommand(string Id) : IRequest<Unit>;
