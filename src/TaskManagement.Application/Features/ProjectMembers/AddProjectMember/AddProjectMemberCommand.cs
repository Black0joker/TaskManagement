using MediatR;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Features.ProjectMembers.AddProjectMember;

public sealed record AddProjectMemberCommand(
    string ProjectId,
    string UserId,
    ProjectMemberRole Role) : IRequest<ProjectMemberResponse>;
