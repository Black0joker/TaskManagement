using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Features.ProjectMembers;

public sealed record ProjectMemberResponse(
    string UserId,
    string Email,
    string FirstName,
    string LastName,
    ProjectMemberRole Role);
