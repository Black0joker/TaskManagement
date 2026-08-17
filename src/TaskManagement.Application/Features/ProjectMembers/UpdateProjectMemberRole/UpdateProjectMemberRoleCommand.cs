using MediatR;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Features.ProjectMembers.UpdateProjectMemberRole;

public sealed record UpdateProjectMemberRoleCommand(
    string ProjectId,
    string UserId,
    ProjectMemberRole Role) : IRequest<ProjectMemberResponse>;
