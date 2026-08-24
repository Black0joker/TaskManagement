namespace TaskManagement.Application.Abstractions.Identity;

public interface IIdentityService
{
    Task<ApplicationUserDto?> GetUserByEmailAsync(string email);
    Task<ApplicationUserDto?> GetUserByIdAsync(string userId);
    Task<IdentityOperationResult> CreateUserAsync(CreateApplicationUserRequest request, string password);
    Task<CredentialValidationResult> ValidateCredentialsAsync(string email, string password);
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

/// <summary>
/// Outcome of a login credential check. Distinguishes invalid credentials from
/// a locked-out account so the caller can respond appropriately.
/// </summary>
public sealed record CredentialValidationResult
{
    public bool Succeeded { get; private init; }
    public bool IsLockedOut { get; private init; }
    public ApplicationUserDto? User { get; private init; }

    public static CredentialValidationResult Success(ApplicationUserDto user) =>
        new() { Succeeded = true, User = user };

    public static CredentialValidationResult Invalid() =>
        new();

    public static CredentialValidationResult LockedOut() =>
        new() { IsLockedOut = true };
}
