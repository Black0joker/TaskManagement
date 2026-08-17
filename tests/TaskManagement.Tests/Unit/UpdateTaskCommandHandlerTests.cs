using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Features.Tasks.UpdateTask;
using TaskManagement.Domain.Enums;
using TaskManagement.Tests.Unit.Testing;
using ValidationException = TaskManagement.Application.Common.Exceptions.ValidationException;

namespace TaskManagement.Tests.Unit;

public class UpdateTaskCommandHandlerTests : HandlerTestBase
{
    private readonly UpdateTaskCommandHandler _handler;

    public UpdateTaskCommandHandlerTests()
    {
        _handler = new UpdateTaskCommandHandler(Context, ProjectAccess);
    }

    [Fact]
    public async Task Handle_UpdatesAllFields_WhenUserCanContribute()
    {
        var owner = await AddUserAsync("owner-1");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        var task = await AddTaskAsync("task-1", "project-1", owner.Id, title: "Original");
        CurrentUser.UserId = owner.Id;

        var due = DateTime.UtcNow.AddDays(5);
        var result = await _handler.Handle(new UpdateTaskCommand(
            task.Id, "Renamed", "New description", TaskItemStatus.InProgress, TaskItemPriority.High, null, due),
            CancellationToken.None);

        Assert.Equal("Renamed", result.Title);
        Assert.Equal("New description", result.Description);
        Assert.Equal(TaskItemStatus.InProgress, result.Status);
        Assert.Equal(TaskItemPriority.High, result.Priority);
        Assert.Null(result.AssignedTo);

        var reloaded = await Context.TaskItems.SingleAsync(t => t.Id == task.Id);
        Assert.Equal("Renamed", reloaded.Title);
    }

    [Fact]
    public async Task Handle_ThrowsNotFound_WhenTaskMissing()
    {
        var owner = await AddUserAsync("owner-1");
        CurrentUser.UserId = owner.Id;

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(new UpdateTaskCommand(
            "missing-task", "T", null, TaskItemStatus.Todo, TaskItemPriority.Low, null, null),
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

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => _handler.Handle(new UpdateTaskCommand(
            task.Id, "Hijacked", null, TaskItemStatus.Todo, TaskItemPriority.Low, null, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ThrowsValidation_WhenAssigneeIsNotProjectMember()
    {
        var owner = await AddUserAsync("owner-1");
        var outsider = await AddUserAsync("outsider-1", "Out", "Sider");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        var task = await AddTaskAsync("task-1", "project-1", owner.Id);
        CurrentUser.UserId = owner.Id;

        await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(new UpdateTaskCommand(
            task.Id, "T", null, TaskItemStatus.Todo, TaskItemPriority.Low, outsider.Id, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ClearsAssignee_WhenAssigneeIsEmpty()
    {
        var owner = await AddUserAsync("owner-1");
        var member = await AddUserAsync("member-1", "Mem", "Ber");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        await AddMemberAsync("project-1", member.Id, ProjectMemberRole.Member);
        var task = await AddTaskAsync("task-1", "project-1", owner.Id, assignedToId: member.Id);
        CurrentUser.UserId = owner.Id;

        var result = await _handler.Handle(new UpdateTaskCommand(
            task.Id, "T", null, TaskItemStatus.Todo, TaskItemPriority.Medium, "   ", null),
            CancellationToken.None);

        Assert.Null(result.AssignedTo);
        var reloaded = await Context.TaskItems.SingleAsync(t => t.Id == task.Id);
        Assert.Null(reloaded.AssignedToId);
    }
}
