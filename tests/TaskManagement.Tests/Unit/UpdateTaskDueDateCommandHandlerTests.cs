using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Features.Tasks.UpdateTaskDueDate;
using TaskManagement.Domain.Enums;
using TaskManagement.Tests.Unit.Testing;

namespace TaskManagement.Tests.Unit;

public class UpdateTaskDueDateCommandHandlerTests : HandlerTestBase
{
    private readonly UpdateTaskDueDateCommandHandler _handler;

    public UpdateTaskDueDateCommandHandlerTests()
    {
        _handler = new UpdateTaskDueDateCommandHandler(Context, ProjectAccess);
    }

    private async Task<string> SeedMemberWithProjectAsync()
    {
        var member = await AddUserAsync("member-1", "Mem", "Ber");
        await AddProjectAsync("project-1", member.Id);
        await AddMemberAsync("project-1", member.Id, ProjectMemberRole.Owner);
        CurrentUser.UserId = member.Id;
        return member.Id;
    }

    [Fact]
    public async Task Handle_UpdatesDueDate_WhenUserCanContribute()
    {
        var ownerId = await SeedMemberWithProjectAsync();
        var task = await AddTaskAsync("task-1", "project-1", ownerId);
        CurrentUser.UserId = ownerId;

        var newDueDate = DateTime.UtcNow.AddDays(10);
        var result = await _handler.Handle(
            new UpdateTaskDueDateCommand(task.Id, newDueDate), CancellationToken.None);

        Assert.Equal(newDueDate, result.DueDate);
    }

    [Fact]
    public async Task Handle_ClearsDueDate_WhenTerminalTaskAllowsIt()
    {
        var ownerId = await SeedMemberWithProjectAsync();
        var task = await AddTaskAsync("task-1", "project-1", ownerId,
            status: TaskItemStatus.Cancelled, dueDate: DateTime.UtcNow.AddDays(2));
        CurrentUser.UserId = ownerId;

        var result = await _handler.Handle(
            new UpdateTaskDueDateCommand(task.Id, null), CancellationToken.None);

        Assert.Null(result.DueDate);
    }

    [Fact]
    public async Task Handle_ThrowsNotFound_WhenTaskMissing()
    {
        await SeedMemberWithProjectAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new UpdateTaskDueDateCommand("missing-task", DateTime.UtcNow.AddDays(1)),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ThrowsForbidden_WhenUserIsNotProjectMember()
    {
        var ownerId = await SeedMemberWithProjectAsync();
        var outsider = await AddUserAsync("outsider-1", "Out", "Sider");
        var task = await AddTaskAsync("task-1", "project-1", ownerId);
        CurrentUser.UserId = outsider.Id;

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _handler.Handle(new UpdateTaskDueDateCommand(task.Id, DateTime.UtcNow.AddDays(1)),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ThrowsBusinessRule_WhenTaskIsDone()
    {
        var ownerId = await SeedMemberWithProjectAsync();
        var task = await AddTaskAsync("task-1", "project-1", ownerId, status: TaskItemStatus.Done);
        CurrentUser.UserId = ownerId;

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _handler.Handle(new UpdateTaskDueDateCommand(task.Id, DateTime.UtcNow.AddDays(1)),
                CancellationToken.None));

        Assert.Contains("immutable", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_ThrowsBusinessRule_WhenClearingDueDateOnActiveTask()
    {
        var ownerId = await SeedMemberWithProjectAsync();
        var task = await AddTaskAsync("task-1", "project-1", ownerId, dueDate: DateTime.UtcNow.AddDays(2));
        CurrentUser.UserId = ownerId;

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _handler.Handle(new UpdateTaskDueDateCommand(task.Id, null), CancellationToken.None));

        Assert.Contains("require a due date", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_ThrowsBusinessRule_WhenSettingDueDateOnCancelledTask()
    {
        var ownerId = await SeedMemberWithProjectAsync();
        var task = await AddTaskAsync("task-1", "project-1", ownerId, status: TaskItemStatus.Cancelled);
        CurrentUser.UserId = ownerId;

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _handler.Handle(new UpdateTaskDueDateCommand(task.Id, DateTime.UtcNow.AddDays(1)),
                CancellationToken.None));

        Assert.Contains("cannot have a due date", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
