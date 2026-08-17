using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TaskManagement.Domain.Authorization;

namespace TaskManagement.Infrastructure.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var roles = context.User.FindAll(ClaimTypes.Role).Select(claim => claim.Value);

        var grantedPermissions = roles
            .SelectMany(role => RolePermissions.For(role))
            .ToHashSet();

        if (grantedPermissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
