namespace TaskManagement.Infrastructure.Identity;

public static class ApplicationRoles
{
    public const string Admin = "Admin";
    public const string User = "User";

    public static readonly string[] All = { Admin, User };
}
