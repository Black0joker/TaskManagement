using MediatR;

namespace TaskManagement.Application.Features.Authentication.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthTokenResponse>;
