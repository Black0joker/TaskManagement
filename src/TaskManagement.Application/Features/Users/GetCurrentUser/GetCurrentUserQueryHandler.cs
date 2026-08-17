using MediatR;
using TaskManagement.Application.Abstractions.Authentication;
using TaskManagement.Application.Abstractions.Identity;
using TaskManagement.Application.Common.Exceptions;

namespace TaskManagement.Application.Features.Users.GetCurrentUser;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;

    public GetCurrentUserQueryHandler(
        ICurrentUserService currentUserService,
        IIdentityService identityService)
    {
        _currentUserService = currentUserService;
        _identityService = identityService;
    }

    public async Task<CurrentUserResponse> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var user = await _identityService.GetUserByIdAsync(_currentUserService.UserId);
        if (user is null)
        {
            throw new NotFoundException("User", _currentUserService.UserId);
        }

        var roles = await _identityService.GetRolesAsync(user.Id);

        return new CurrentUserResponse(user.Id, user.Email, user.FirstName, user.LastName, roles);
    }
}
