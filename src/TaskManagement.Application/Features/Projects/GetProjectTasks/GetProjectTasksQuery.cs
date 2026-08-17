using MediatR;

namespace TaskManagement.Application.Features.Projects.GetProjectTasks;

public sealed record GetProjectTasksQuery(string ProjectId) : IRequest<IReadOnlyList<ProjectTaskSummary>>;
