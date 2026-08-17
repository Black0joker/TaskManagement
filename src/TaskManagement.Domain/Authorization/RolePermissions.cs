namespace TaskManagement.Domain.Authorization;

public static class RolePermissions
{
    private static readonly IReadOnlySet<string> AdminPermissions =
        ApplicationPermissions.All.ToHashSet();

    private static readonly IReadOnlySet<string> UserPermissions = new HashSet<string>
    {
        ApplicationPermissions.Projects.Read,
        ApplicationPermissions.Projects.Create,
        ApplicationPermissions.Projects.Update,
        ApplicationPermissions.Tasks.Read,
        ApplicationPermissions.Tasks.Create,
        ApplicationPermissions.Tasks.Update,
        ApplicationPermissions.Comments.Create,
        ApplicationPermissions.Comments.Update
    };

    private static readonly IReadOnlySet<string> NoPermissions = new HashSet<string>();

    public static IReadOnlySet<string> For(string role) => role switch
    {
        ApplicationRoles.Admin => AdminPermissions,
        ApplicationRoles.User => UserPermissions,
        _ => NoPermissions
    };
}
