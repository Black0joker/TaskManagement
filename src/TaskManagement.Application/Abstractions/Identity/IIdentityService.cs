namespace TaskManagement.Application.Abstractions.Identity;

public interface IIdentityService
{
    Task<ApplicationUserDto?> GetUserByEmailAsync(string email);
    Task<ApplicationUserDto?> GetUserByIdAsync(string userId);
    Task<IdentityOperationResult> CreateUserAsync(CreateApplicationUserRequest request, string password);
    Task<ApplicationUserDto?> ValidateCredentialsAsync(string email, string password);
    Task<IReadOnlyList<string>> GetRolesAsync(string userId);
}

public sealed record ApplicationUserDto(
    string Id,
    string Email,
    string UserName,
    string FirstName,
    string LastName);

public sealed record CreateApplicationUserRequest(
    string Email,
    string UserName,
    string FirstName,
    string LastName);

public sealed record IdentityOperationResult(
    bool Succeeded,
    IReadOnlyList<string> Errors,
    string? CreatedUserId = null)
{
    public static IdentityOperationResult Success(string createdUserId) =>
        new(true, Array.Empty<string>(), createdUserId);

    public static IdentityOperationResult Failure(IEnumerable<string> errors) =>
        new(false, errors.ToArray());
}
