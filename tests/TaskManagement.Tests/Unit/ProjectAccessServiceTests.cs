using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Domain.Authorization;
using TaskManagement.Domain.Enums;
using TaskManagement.Tests.Unit.Testing;

namespace TaskManagement.Tests.Unit;

public class ProjectAccessServiceTests : HandlerTestBase
{
    [Fact]
    public async Task GetRoleAsync_ReturnsNull_WhenUserIsNotAuthenticated()
    {
        CurrentUser.UserId = null;

        Assert.Null(await ProjectAccess.GetRoleAsync("project-1"));
    }

    [Fact]
    public async Task GetRoleAsync_ReturnsOwnerImplicitly_ForSystemAdmins()
    {
        var admin = await AddUserAsync("admin-1", "Sys", "Admin");
        CurrentUser.UserId = admin.Id;
        CurrentUser.AddRole(ApplicationRoles.Admin);

        // No membership rows exist for this project at all.
        var owner = await AddUserAsync("owner-1");
        await AddProjectAsync("project-1", owner.Id);

        Assert.Equal(ProjectMemberRole.Owner, await ProjectAccess.GetRoleAsync("project-1"));
        Assert.True(await ProjectAccess.CanReadAsync("project-1"));
        Assert.True(await ProjectAccess.CanContributeAsync("project-1"));
        Assert.True(await ProjectAccess.CanManageAsync("project-1"));
    }

    [Fact]
    public async Task GetRoleAsync_ReturnsMembershipRole_ForRegularUsers()
    {
        var owner = await AddUserAsync("owner-1");
        var member = await AddUserAsync("member-1", "Mem", "Ber");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        await AddMemberAsync("project-1", member.Id, ProjectMemberRole.Member);
        CurrentUser.UserId = member.Id;

        Assert.Equal(ProjectMemberRole.Member, await ProjectAccess.GetRoleAsync("project-1"));
    }

    [Fact]
    public async Task GetRoleAsync_ReturnsNull_WhenUserIsNotAMember()
    {
        var owner = await AddUserAsync("owner-1");
        var outsider = await AddUserAsync("outsider-1", "Out", "Sider");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        CurrentUser.UserId = outsider.Id;

        Assert.Null(await ProjectAccess.GetRoleAsync("project-1"));
        Assert.False(await ProjectAccess.CanReadAsync("project-1"));
        Assert.False(await ProjectAccess.CanContributeAsync("project-1"));
        Assert.False(await ProjectAccess.CanManageAsync("project-1"));
    }

    [Theory]
    [InlineData(ProjectMemberRole.Owner, true, true, true)]
    [InlineData(ProjectMemberRole.Admin, true, true, true)]
    [InlineData(ProjectMemberRole.Member, true, true, false)]
    [InlineData(ProjectMemberRole.Viewer, true, false, false)]
    public async Task Permissions_FollowRoleMatrix(
        ProjectMemberRole role,
        bool canRead,
        bool canContribute,
        bool canManage)
    {
        var owner = await AddUserAsync("owner-1");
        var subject = await AddUserAsync("subject-1", "Sub", "Ject");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", subject.Id, role);
        CurrentUser.UserId = subject.Id;

        IProjectAccessService service = ProjectAccess;

        Assert.Equal(canRead, await service.CanReadAsync("project-1"));
        Assert.Equal(canContribute, await service.CanContributeAsync("project-1"));
        Assert.Equal(canManage, await service.CanManageAsync("project-1"));
    }
}
