using Microsoft.Extensions.Logging.Abstractions;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Features.Tasks.UpdateTaskStatus;
using TaskManagement.Domain.Enums;
using TaskManagement.Tests.Unit.Testing;
using ValidationException = TaskManagement.Application.Common.Exceptions.ValidationException;

namespace TaskManagement.Tests.Unit;

public class UpdateTaskStatusCommandHandlerTests : HandlerTestBase
{
    private readonly UpdateTaskStatusCommandHandler _handler;

    public UpdateTaskStatusCommandHandlerTests()
    {
        _handler = new UpdateTaskStatusCommandHandler(
            Context, ProjectAccess, CurrentUser, NullLogger<UpdateTaskStatusCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_AllowsForwardTransition_ForAnyContributor()
    {
        var owner = await AddUserAsync("owner-1");
        var member = await AddUserAsync("member-1", "Mem", "Ber");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        await AddMemberAsync("project-1", member.Id, ProjectMemberRole.Member);
        var task = await AddTaskAsync("task-1", "project-1", owner.Id, status: TaskItemStatus.Todo);
        CurrentUser.UserId = member.Id;

        var result = await _handler.Handle(
            new UpdateTaskStatusCommand(task.Id, TaskItemStatus.InProgress),
            CancellationToken.None);

        Assert.Equal(TaskItemStatus.InProgress, result.Status);
    }

    [Fact]
    public async Task Handle_ThrowsValidation_WhenStatusUnchanged()
    {
        var owner = await AddUserAsync("owner-1");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        var task = await AddTaskAsync("task-1", "project-1", owner.Id, status: TaskItemStatus.InProgress);
        CurrentUser.UserId = owner.Id;

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(
            new UpdateTaskStatusCommand(task.Id, TaskItemStatus.InProgress),
            CancellationToken.None));

        Assert.Contains(ex.Errors.Values.SelectMany(v => v), m => m.Contains("already in status"));
    }

    [Fact]
    public async Task Handle_ThrowsNotFound_WhenTaskMissing()
    {
        var owner = await AddUserAsync("owner-1");
        CurrentUser.UserId = owner.Id;

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(
            new UpdateTaskStatusCommand("missing-task", TaskItemStatus.Done),
            CancellationToken.None));
    }

    [Theory]
    [InlineData(TaskItemStatus.InReview, TaskItemStatus.Todo)]        // rework
    [InlineData(TaskItemStatus.Done, TaskItemStatus.InProgress)]      // reopen
    [InlineData(TaskItemStatus.Cancelled, TaskItemStatus.Todo)]       // resurrect
    [InlineData(TaskItemStatus.Done, TaskItemStatus.Cancelled)]       // cancel completed work
    public async Task Handle_ThrowsForbidden_WhenNonManagerMovesBackwards(
        TaskItemStatus from,
        TaskItemStatus to)
    {
        var owner = await AddUserAsync("owner-1");
        var member = await AddUserAsync("member-1", "Mem", "Ber");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        await AddMemberAsync("project-1", member.Id, ProjectMemberRole.Member);
        var task = await AddTaskAsync("task-1", "project-1", owner.Id, status: from);
        CurrentUser.UserId = member.Id;

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => _handler.Handle(
            new UpdateTaskStatusCommand(task.Id, to),
            CancellationToken.None));
    }

    [Theory]
    [InlineData(ProjectMemberRole.Owner)]
    [InlineData(ProjectMemberRole.Admin)]
    public async Task Handle_AllowsBackwardTransition_ForManagers(ProjectMemberRole role)
    {
        var owner = await AddUserAsync("owner-1");
        var manager = await AddUserAsync("manager-1", "Man", "Ager");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        await AddMemberAsync("project-1", manager.Id, role);
        var task = await AddTaskAsync("task-1", "project-1", owner.Id, status: TaskItemStatus.Done);
        CurrentUser.UserId = manager.Id;

        var result = await _handler.Handle(
            new UpdateTaskStatusCommand(task.Id, TaskItemStatus.InProgress),
            CancellationToken.None);

        Assert.Equal(TaskItemStatus.InProgress, result.Status);
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
            new UpdateTaskStatusCommand(task.Id, TaskItemStatus.InProgress),
            CancellationToken.None));
    }
}
