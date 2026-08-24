using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Features.Tasks.UpdateTaskPriority;
using TaskManagement.Domain.Enums;
using TaskManagement.Tests.Unit.Testing;

namespace TaskManagement.Tests.Unit;

public class UpdateTaskPriorityCommandHandlerTests : HandlerTestBase
{
    private readonly UpdateTaskPriorityCommandHandler _handler;

    public UpdateTaskPriorityCommandHandlerTests()
    {
        _handler = new UpdateTaskPriorityCommandHandler(Context, ProjectAccess);
    }

    [Fact]
    public async Task Handle_ChangesPriority_WhenUserCanContribute()
    {
        var owner = await AddUserAsync("owner-1");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        var task = await AddTaskAsync("task-1", "project-1", owner.Id, priority: TaskItemPriority.Low);
        CurrentUser.UserId = owner.Id;

        var result = await _handler.Handle(
            new UpdateTaskPriorityCommand(task.Id, TaskItemPriority.Critical),
            CancellationToken.None);

        Assert.Equal(TaskItemPriority.Critical, result.Priority);
        var reloaded = await Context.TaskItems.SingleAsync(t => t.Id == task.Id);
        Assert.Equal(TaskItemPriority.Critical, reloaded.Priority);
    }

    [Fact]
    public async Task Handle_IsIdempotent_WhenPriorityUnchanged()
    {
        var owner = await AddUserAsync("owner-1");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        var task = await AddTaskAsync("task-1", "project-1", owner.Id, priority: TaskItemPriority.High);
        CurrentUser.UserId = owner.Id;

        var result = await _handler.Handle(
            new UpdateTaskPriorityCommand(task.Id, TaskItemPriority.High),
            CancellationToken.None);

        Assert.Equal(TaskItemPriority.High, result.Priority);
    }

    [Fact]
    public async Task Handle_ThrowsNotFound_WhenTaskMissing()
    {
        var owner = await AddUserAsync("owner-1");
        CurrentUser.UserId = owner.Id;

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(
            new UpdateTaskPriorityCommand("missing-task", TaskItemPriority.High),
            CancellationToken.None));
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

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => _handler.Handle(
            new UpdateTaskPriorityCommand(task.Id, TaskItemPriority.Critical),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ThrowsBusinessRule_WhenTaskIsDone()
    {
        var owner = await AddUserAsync("owner-1");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        var task = await AddTaskAsync("task-1", "project-1", owner.Id,
            status: TaskItemStatus.Done, priority: TaskItemPriority.Medium);
        CurrentUser.UserId = owner.Id;

        await Assert.ThrowsAsync<BusinessRuleException>(() => _handler.Handle(
            new UpdateTaskPriorityCommand(task.Id, TaskItemPriority.Critical),
            CancellationToken.None));
    }
}
