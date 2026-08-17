using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Features.Tasks.CreateTask;
using TaskManagement.Domain.Enums;
using TaskManagement.Tests.Unit.Testing;
using ValidationException = TaskManagement.Application.Common.Exceptions.ValidationException;

namespace TaskManagement.Tests.Unit;

public class CreateTaskCommandHandlerTests : HandlerTestBase
{
    private readonly CreateTaskCommandHandler _handler;

    public CreateTaskCommandHandlerTests()
    {
        _handler = new CreateTaskCommandHandler(Context, ProjectAccess, CurrentUser);
    }

    [Fact]
    public async Task Handle_CreatesTask_WhenUserIsProjectMember()
    {
        var owner = await AddUserAsync("owner-1");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        CurrentUser.UserId = owner.Id;

        var result = await _handler.Handle(new CreateTaskCommand(
            "project-1", "New task", "Details", TaskItemStatus.Todo, TaskItemPriority.High, null, null),
            CancellationToken.None);

        Assert.NotEmpty(result.Id);
        Assert.Equal("project-1", result.ProjectId);
        Assert.Equal("New task", result.Title);
        Assert.Equal(TaskItemStatus.Todo, result.Status);
        Assert.Equal(TaskItemPriority.High, result.Priority);
        Assert.Equal(owner.Id, result.CreatedById);
        Assert.Equal(1, await Context.TaskItems.CountAsync());
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorized_WhenNoAuthenticatedUser()
    {
        CurrentUser.UserId = null;

        await Assert.ThrowsAsync<UnauthorizedException>(() => _handler.Handle(
            new CreateTaskCommand("project-1", "T", null, TaskItemStatus.Todo, TaskItemPriority.Low, null, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ThrowsNotFound_WhenProjectMissing()
    {
        var user = await AddUserAsync("user-1");
        CurrentUser.UserId = user.Id;

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(
            new CreateTaskCommand("missing-project", "T", null, TaskItemStatus.Todo, TaskItemPriority.Low, null, null),
            CancellationToken.None));

        Assert.Contains("missing-project", ex.Message);
    }

    [Fact]
    public async Task Handle_ThrowsForbidden_WhenUserIsNotProjectMember()
    {
        var owner = await AddUserAsync("owner-1");
        var outsider = await AddUserAsync("outsider-1", "Out", "Sider");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        CurrentUser.UserId = outsider.Id;

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => _handler.Handle(
            new CreateTaskCommand("project-1", "T", null, TaskItemStatus.Todo, TaskItemPriority.Low, null, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ThrowsValidation_WhenAssigneeIsNotProjectMember()
    {
        var owner = await AddUserAsync("owner-1");
        var outsider = await AddUserAsync("outsider-1", "Out", "Sider");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        CurrentUser.UserId = owner.Id;

        await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(
            new CreateTaskCommand("project-1", "T", null, TaskItemStatus.Todo, TaskItemPriority.Low, outsider.Id, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ResolvesAssignee_WhenAssigneeIsProjectMember()
    {
        var owner = await AddUserAsync("owner-1");
        var member = await AddUserAsync("member-1", "Mem", "Ber");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        await AddMemberAsync("project-1", member.Id, ProjectMemberRole.Member);
        CurrentUser.UserId = owner.Id;

        var result = await _handler.Handle(new CreateTaskCommand(
            "project-1", "T", null, TaskItemStatus.Todo, TaskItemPriority.Low, member.Id, null),
            CancellationToken.None);

        Assert.NotNull(result.AssignedTo);
        Assert.Equal(member.Id, result.AssignedTo!.Id);
        Assert.Equal("Mem Ber", result.AssignedTo.Name);
    }
}
