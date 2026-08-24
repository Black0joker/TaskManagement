using Microsoft.AspNetCore.Identity;
using TaskManagement.Application.Abstractions.Identity;
using TaskManagement.Domain.Authorization;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<User> _userManager;

    public IdentityService(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<ApplicationUserDto?> GetUserByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user is null ? null : ToDto(user);
    }

    public async Task<ApplicationUserDto?> GetUserByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user is null ? null : ToDto(user);
    }

    public async Task<IdentityOperationResult> CreateUserAsync(CreateApplicationUserRequest request, string password)
    {
        var user = new User
        {
            UserName = request.UserName,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = true,
            LockoutEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return IdentityOperationResult.Failure(result.Errors.Select(e => e.Description));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, ApplicationRoles.User);
        if (!roleResult.Succeeded)
        {
            return IdentityOperationResult.Failure(roleResult.Errors.Select(e => e.Description));
        }

        return IdentityOperationResult.Success(user.Id);
    }

    public async Task<CredentialValidationResult> ValidateCredentialsAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return CredentialValidationResult.Invalid();
        }

        // A locked-out account is rejected without checking the password so the
        // lockout duration cannot be probed or shortened with correct guesses.
        if (await _userManager.IsLockedOutAsync(user))
        {
            return CredentialValidationResult.LockedOut();
        }

        if (await _userManager.CheckPasswordAsync(user, password))
        {
            // Successful login resets the failed-attempt counter.
            await _userManager.ResetAccessFailedCountAsync(user);
            return CredentialValidationResult.Success(ToDto(user));
        }

        // Increments the per-account counter and locks the account once
        // MaxFailedAccessAttempts is reached (see LockoutOptions).
        await _userManager.AccessFailedAsync(user);
        return CredentialValidationResult.Invalid();
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Array.Empty<string>();
        }

        var roles = await _userManager.GetRolesAsync(user);
        return roles.ToList();
    }

    private static ApplicationUserDto ToDto(User user) => new(
        user.Id,
        user.Email ?? string.Empty,
        user.UserName ?? string.Empty,
        user.FirstName,
        user.LastName);
}
