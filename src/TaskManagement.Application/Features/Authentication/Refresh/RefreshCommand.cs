using MediatR;

namespace TaskManagement.Application.Features.Authentication.Refresh;

public sealed record RefreshCommand(string RefreshToken) : IRequest<AuthTokenResponse>;
