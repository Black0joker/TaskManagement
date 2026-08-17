using MediatR;

namespace TaskManagement.Application.Features.ProjectMembers.GetProjectMembers;

public sealed record GetProjectMembersQuery(string ProjectId) : IRequest<IReadOnlyList<ProjectMemberResponse>>;
