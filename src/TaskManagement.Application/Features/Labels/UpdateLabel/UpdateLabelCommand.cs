using MediatR;
using TaskManagement.Application.Features.Projects;

namespace TaskManagement.Application.Features.Labels.UpdateLabel;

public sealed record UpdateLabelCommand(
    string Id,
    string Name,
    string Color) : IRequest<ProjectLabelSummary>;
