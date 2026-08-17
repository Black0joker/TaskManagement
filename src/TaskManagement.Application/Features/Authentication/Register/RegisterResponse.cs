namespace TaskManagement.Application.Features.Authentication.Register;

public sealed record RegisterResponse(
    string Id,
    string Email,
    string FirstName,
    string LastName);
