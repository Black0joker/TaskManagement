using FluentValidation.Results;
using MediatR;
using TaskManagement.Application.Abstractions.Identity;
using TaskManagement.Application.Common.Exceptions;

namespace TaskManagement.Application.Features.Authentication.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private readonly IIdentityService _identityService;

    public RegisterCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existing = await _identityService.GetUserByEmailAsync(request.Email);
        if (existing is not null)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(
                    nameof(request.Email),
                    "A user with this email already exists.")
            });
        }

        var createRequest = new CreateApplicationUserRequest(
            request.Email,
            request.Email,
            request.FirstName,
            request.LastName);

        var result = await _identityService.CreateUserAsync(createRequest, request.Password);

        if (!result.Succeeded)
        {
            throw new ValidationException(
                result.Errors.Select(e => new ValidationFailure(nameof(request.Password), e)));
        }

        var user = await _identityService.GetUserByIdAsync(result.CreatedUserId!);

        return new RegisterResponse(user!.Id, user.Email, user.FirstName, user.LastName);
    }
}
