using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Features.Projects.DeleteProject;
using TaskManagement.Domain.Enums;
using TaskManagement.Tests.Unit.Testing;

namespace TaskManagement.Tests.Unit;

public class DeleteProjectCommandHandlerTests : HandlerTestBase
{
    private readonly DeleteProjectCommandHandler _handler;

    public DeleteProjectCommandHandlerTests()
    {
        _handler = new DeleteProjectCommandHandler(Context, ProjectAccess);
    }

    [Fact]
    public async Task Handle_RemovesProject_WhenUserIsOwner()
    {
        var owner = await AddUserAsync("owner-1");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        CurrentUser.UserId = owner.Id;

        await _handler.Handle(new DeleteProjectCommand("project-1"), CancellationToken.None);

        Assert.False(await Context.Projects.AnyAsync(p => p.Id == "project-1"));
    }

    [Fact]
    public async Task Handle_RemovesProjectLabels_AlongWithProject()
    {
        var owner = await AddUserAsync("owner-1");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);

        Context.Labels.Add(new Domain.Entities.Label
        {
            ProjectId = "project-1",
            Name = "Backend",
            Color = "#3B82F6"
        });
        await Context.SaveChangesAsync();

        CurrentUser.UserId = owner.Id;

        await _handler.Handle(new DeleteProjectCommand("project-1"), CancellationToken.None);

        Assert.False(await Context.Labels.AnyAsync(l => l.ProjectId == "project-1"));
    }

    [Fact]
    public async Task Handle_ThrowsNotFound_WhenProjectMissing()
    {
        var owner = await AddUserAsync("owner-1");
        CurrentUser.UserId = owner.Id;

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new DeleteProjectCommand("missing-project"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ThrowsForbidden_WhenUserIsProjectAdmin()
    {
        var owner = await AddUserAsync("owner-1");
        var admin = await AddUserAsync("admin-1", "Ad", "Min");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        await AddMemberAsync("project-1", admin.Id, ProjectMemberRole.Admin);
        CurrentUser.UserId = admin.Id;

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _handler.Handle(new DeleteProjectCommand("project-1"), CancellationToken.None));

        // Project still exists.
        Assert.True(await Context.Projects.AnyAsync(p => p.Id == "project-1"));
    }

    [Fact]
    public async Task Handle_ThrowsForbidden_WhenUserIsNotMember()
    {
        var owner = await AddUserAsync("owner-1");
        var outsider = await AddUserAsync("outsider-1", "Out", "Sider");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        CurrentUser.UserId = outsider.Id;

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _handler.Handle(new DeleteProjectCommand("project-1"), CancellationToken.None));
    }
}
