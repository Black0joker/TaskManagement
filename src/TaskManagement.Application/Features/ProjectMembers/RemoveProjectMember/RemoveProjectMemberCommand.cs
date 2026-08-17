using MediatR;

namespace TaskManagement.Application.Features.ProjectMembers.RemoveProjectMember;

public sealed record RemoveProjectMemberCommand(string ProjectId, string UserId) : IRequest;
