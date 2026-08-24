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
        var task = await AddTaskAsync("task-1", "project-1", owner.Id, title: "Original", assignedToId: owner.Id);
        CurrentUser.UserId = owner.Id;

        var due = DateTime.UtcNow.AddDays(5);
        var result = await _handler.Handle(new UpdateTaskCommand(
            task.Id, "Renamed", "New description", TaskItemStatus.InProgress, TaskItemPriority.High, owner.Id, due),
            CancellationToken.None);

        Assert.Equal("Renamed", result.Title);
        Assert.Equal("New description", result.Description);
        Assert.Equal(TaskItemStatus.InProgress, result.Status);
        Assert.Equal(TaskItemPriority.High, result.Priority);

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
        var dueDate = DateTime.UtcNow.AddDays(5);
        var task = await AddTaskAsync("task-1", "project-1", owner.Id, dueDate: dueDate);
        CurrentUser.UserId = owner.Id;

        await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(new UpdateTaskCommand(
            task.Id, "T", null, TaskItemStatus.Todo, TaskItemPriority.Low, outsider.Id, dueDate),
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
        var dueDate = DateTime.UtcNow.AddDays(5);
        var task = await AddTaskAsync("task-1", "project-1", owner.Id, assignedToId: member.Id, dueDate: dueDate);
        CurrentUser.UserId = owner.Id;

        var result = await _handler.Handle(new UpdateTaskCommand(
            task.Id, "T", null, TaskItemStatus.Todo, TaskItemPriority.Medium, "   ", dueDate),
            CancellationToken.None);

        Assert.Null(result.AssignedTo);
        var reloaded = await Context.TaskItems.SingleAsync(t => t.Id == task.Id);
        Assert.Null(reloaded.AssignedToId);
    }

    [Fact]
    public async Task Handle_ThrowsBusinessRule_WhenDoneTaskIsModified()
    {
        var owner = await AddUserAsync("owner-1");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        var task = await AddTaskAsync("task-1", "project-1", owner.Id,
            status: TaskItemStatus.Done, assignedToId: owner.Id);
        CurrentUser.UserId = owner.Id;

        // Changing priority on a Done task should be rejected.
        await Assert.ThrowsAsync<BusinessRuleException>(() => _handler.Handle(new UpdateTaskCommand(
            task.Id, "T", null, TaskItemStatus.Done, TaskItemPriority.High, owner.Id, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AllowsTitleChange_OnDoneTask()
    {
        var owner = await AddUserAsync("owner-1");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        var task = await AddTaskAsync("task-1", "project-1", owner.Id,
            status: TaskItemStatus.Done, priority: TaskItemPriority.Medium, assignedToId: owner.Id);
        CurrentUser.UserId = owner.Id;

        // Title and description changes are still allowed on Done tasks.
        var result = await _handler.Handle(new UpdateTaskCommand(
            task.Id, "Updated Title", "New desc", TaskItemStatus.Done, TaskItemPriority.Medium, owner.Id, null),
            CancellationToken.None);

        Assert.Equal("Updated Title", result.Title);
    }

    [Fact]
    public async Task Handle_ThrowsForbidden_WhenMemberMovesTaskBackward()
    {
        var owner = await AddUserAsync("owner-1");
        var member = await AddUserAsync("member-1", "Mem", "Ber");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        await AddMemberAsync("project-1", member.Id, ProjectMemberRole.Member);
        var task = await AddTaskAsync("task-1", "project-1", owner.Id,
            status: TaskItemStatus.InReview, assignedToId: member.Id);
        CurrentUser.UserId = member.Id;

        // Moving from InReview back to Todo is a backward transition.
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => _handler.Handle(new UpdateTaskCommand(
            task.Id, "T", null, TaskItemStatus.Todo, TaskItemPriority.Medium, member.Id, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AllowsBackwardTransition_WhenUserIsProjectAdmin()
    {
        var owner = await AddUserAsync("owner-1");
        var admin = await AddUserAsync("admin-1", "Ad", "Min");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        await AddMemberAsync("project-1", admin.Id, ProjectMemberRole.Admin);
        var dueDate = DateTime.UtcNow.AddDays(5);
        var task = await AddTaskAsync("task-1", "project-1", owner.Id,
            status: TaskItemStatus.InReview, assignedToId: admin.Id, dueDate: dueDate);
        CurrentUser.UserId = admin.Id;

        var result = await _handler.Handle(new UpdateTaskCommand(
            task.Id, "T", null, TaskItemStatus.Todo, TaskItemPriority.Medium, admin.Id, dueDate),
            CancellationToken.None);

        Assert.Equal(TaskItemStatus.Todo, result.Status);
    }

    [Fact]
    public async Task Handle_ThrowsBusinessRule_WhenUnassignedTaskMovesToInProgress()
    {
        var owner = await AddUserAsync("owner-1");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        var task = await AddTaskAsync("task-1", "project-1", owner.Id, assignedToId: null);
        CurrentUser.UserId = owner.Id;

        await Assert.ThrowsAsync<BusinessRuleException>(() => _handler.Handle(new UpdateTaskCommand(
            task.Id, "T", null, TaskItemStatus.InProgress, TaskItemPriority.Medium, null, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AllowsInProgress_WhenAssigneeProvidedInSameRequest()
    {
        var owner = await AddUserAsync("owner-1");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        var task = await AddTaskAsync("task-1", "project-1", owner.Id, assignedToId: null);
        CurrentUser.UserId = owner.Id;

        var result = await _handler.Handle(new UpdateTaskCommand(
            task.Id, "T", null, TaskItemStatus.InProgress, TaskItemPriority.Medium, owner.Id,
            DateTime.UtcNow.AddDays(5)),
            CancellationToken.None);

        Assert.Equal(TaskItemStatus.InProgress, result.Status);
    }

    [Fact]
    public async Task Handle_ThrowsBusinessRule_WhenActiveTaskLosesItsDueDate()
    {
        var owner = await AddUserAsync("owner-1");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        var task = await AddTaskAsync("task-1", "project-1", owner.Id,
            status: TaskItemStatus.InReview, assignedToId: owner.Id, dueDate: DateTime.UtcNow.AddDays(3));
        CurrentUser.UserId = owner.Id;

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _handler.Handle(new UpdateTaskCommand(
            task.Id, "T", null, TaskItemStatus.InReview, TaskItemPriority.Medium, owner.Id, null),
            CancellationToken.None));

        Assert.Contains("require a due date", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_ThrowsBusinessRule_WhenTerminalTaskCarriesDueDate()
    {
        var owner = await AddUserAsync("owner-1");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        var task = await AddTaskAsync("task-1", "project-1", owner.Id,
            status: TaskItemStatus.InReview, assignedToId: owner.Id, dueDate: DateTime.UtcNow.AddDays(3));
        CurrentUser.UserId = owner.Id;

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _handler.Handle(new UpdateTaskCommand(
            task.Id, "T", null, TaskItemStatus.Done, TaskItemPriority.Medium, owner.Id,
            DateTime.UtcNow.AddDays(5)),
            CancellationToken.None));

        Assert.Contains("cannot have a due date", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_DropsDueDate_WhenMovingToTerminalStatus()
    {
        var owner = await AddUserAsync("owner-1");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        var task = await AddTaskAsync("task-1", "project-1", owner.Id,
            status: TaskItemStatus.InReview, assignedToId: owner.Id, dueDate: DateTime.UtcNow.AddDays(3));
        CurrentUser.UserId = owner.Id;

        var result = await _handler.Handle(new UpdateTaskCommand(
            task.Id, "T", null, TaskItemStatus.Done, TaskItemPriority.Medium, owner.Id, null),
            CancellationToken.None);

        Assert.Equal(TaskItemStatus.Done, result.Status);
        Assert.Null(result.DueDate);
    }
}
