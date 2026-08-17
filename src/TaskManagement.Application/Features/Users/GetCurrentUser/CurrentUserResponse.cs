namespace TaskManagement.Application.Features.Users.GetCurrentUser;

public sealed record CurrentUserResponse(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Roles);
