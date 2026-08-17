using MediatR;

namespace TaskManagement.Application.Features.Users.GetCurrentUser;

public sealed record GetCurrentUserQuery : IRequest<CurrentUserResponse>;
