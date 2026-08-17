using MediatR;

namespace TaskManagement.Application.Features.Projects.GetProjectLabels;

public sealed record GetProjectLabelsQuery(string ProjectId) : IRequest<IReadOnlyList<ProjectLabelSummary>>;
