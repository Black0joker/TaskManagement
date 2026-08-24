using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Features.Tasks.DeleteTask;
using TaskManagement.Domain.Enums;
using TaskManagement.Tests.Unit.Testing;

namespace TaskManagement.Tests.Unit;

public class DeleteTaskCommandHandlerTests : HandlerTestBase
{
    private readonly DeleteTaskCommandHandler _handler;

    public DeleteTaskCommandHandlerTests()
    {
        _handler = new DeleteTaskCommandHandler(Context, ProjectAccess);
    }

    [Fact]
    public async Task Handle_RemovesTask_WhenUserIsProjectOwner()
    {
        var owner = await AddUserAsync("owner-1");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        var task = await AddTaskAsync("task-1", "project-1", owner.Id);
        CurrentUser.UserId = owner.Id;

        await _handler.Handle(new DeleteTaskCommand(task.Id), CancellationToken.None);

        Assert.False(await Context.TaskItems.AnyAsync(t => t.Id == task.Id));
    }

    [Fact]
    public async Task Handle_RemovesTask_WhenUserIsProjectAdmin()
    {
        var owner = await AddUserAsync("owner-1");
        var admin = await AddUserAsync("admin-1", "Ad", "Min");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        await AddMemberAsync("project-1", admin.Id, ProjectMemberRole.Admin);
        var task = await AddTaskAsync("task-1", "project-1", owner.Id);
        CurrentUser.UserId = admin.Id;

        await _handler.Handle(new DeleteTaskCommand(task.Id), CancellationToken.None);

        Assert.False(await Context.TaskItems.AnyAsync(t => t.Id == task.Id));
    }

    [Fact]
    public async Task Handle_ThrowsNotFound_WhenTaskMissing()
    {
        var owner = await AddUserAsync("owner-1");
        CurrentUser.UserId = owner.Id;

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new DeleteTaskCommand("missing-task"), CancellationToken.None));

        Assert.Contains("missing-task", ex.Message);
    }

    [Fact]
    public async Task Handle_ThrowsForbidden_WhenUserIsOnlyMember()
    {
        var owner = await AddUserAsync("owner-1");
        var member = await AddUserAsync("member-1", "Mem", "Ber");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        await AddMemberAsync("project-1", member.Id, ProjectMemberRole.Member);
        var task = await AddTaskAsync("task-1", "project-1", owner.Id);
        CurrentUser.UserId = member.Id;

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _handler.Handle(new DeleteTaskCommand(task.Id), CancellationToken.None));

        // Task still exists.
        Assert.True(await Context.TaskItems.AnyAsync(t => t.Id == task.Id));
    }

    [Fact]
    public async Task Handle_ThrowsForbidden_WhenUserIsNotProjectMember()
    {
        var owner = await AddUserAsync("owner-1");
        var outsider = await AddUserAsync("outsider-1", "Out", "Sider");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        var task = await AddTaskAsync("task-1", "project-1", owner.Id);
        CurrentUser.UserId = outsider.Id;

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _handler.Handle(new DeleteTaskCommand(task.Id), CancellationToken.None));
    }
}
