using MediatR;
using TaskManagement.Application.Features.Projects;

namespace TaskManagement.Application.Features.Labels.CreateProjectLabel;

public sealed record CreateProjectLabelCommand(
    string ProjectId,
    string Name,
    string Color) : IRequest<ProjectLabelSummary>;
