using MediatR;

namespace TaskManagement.Application.Features.Authentication.Logout;

public sealed record LogoutCommand(string RefreshToken) : IRequest;
