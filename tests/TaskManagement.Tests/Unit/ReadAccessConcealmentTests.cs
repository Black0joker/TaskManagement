using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Features.Projects.GetProject;
using TaskManagement.Application.Features.Tasks.GetTask;
using TaskManagement.Domain.Enums;
using TaskManagement.Tests.Unit.Testing;

namespace TaskManagement.Tests.Unit;

/// <summary>
/// Inaccessible resources must be indistinguishable from missing ones (404 in
/// both cases) so valid task/project IDs cannot be enumerated by probing.
/// </summary>
public class ReadAccessConcealmentTests : HandlerTestBase
{
    private readonly GetTaskQueryHandler _taskHandler;
    private readonly GetProjectQueryHandler _projectHandler;

    public ReadAccessConcealmentTests()
    {
        _taskHandler = new GetTaskQueryHandler(Context, ProjectAccess);
        _projectHandler = new GetProjectQueryHandler(Context, ProjectAccess);
    }

    [Fact]
    public async Task GetTask_ReturnsNotFound_ForInaccessibleTask_IndistinguishableFromMissing()
    {
        var owner = await AddUserAsync("owner-1");
        var outsider = await AddUserAsync("outsider-1", "Out", "Sider");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        await AddTaskAsync("task-1", "project-1", owner.Id);
        CurrentUser.UserId = outsider.Id;

        var inaccessible = await Assert.ThrowsAsync<NotFoundException>(() =>
            _taskHandler.Handle(new GetTaskQuery("task-1"), CancellationToken.None));

        // A genuinely missing ID throws the same exception type.
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _taskHandler.Handle(new GetTaskQuery("no-such-task"), CancellationToken.None));

        // The message echoes only the caller-supplied ID, so the unauthorized
        // response is byte-identical to the not-found response for that ID.
        Assert.Equal(new NotFoundException("Task", "task-1").Message, inaccessible.Message);
    }

    [Fact]
    public async Task GetTask_ReturnsTask_WhenUserIsProjectMember()
    {
        var owner = await AddUserAsync("owner-1");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        await AddTaskAsync("task-1", "project-1", owner.Id, title: "Visible task");
        CurrentUser.UserId = owner.Id;

        var result = await _taskHandler.Handle(new GetTaskQuery("task-1"), CancellationToken.None);

        Assert.Equal("task-1", result.Id);
        Assert.Equal("Visible task", result.Title);
    }

    [Fact]
    public async Task GetProject_ReturnsNotFound_ForInaccessibleProject_IndistinguishableFromMissing()
    {
        var owner = await AddUserAsync("owner-1");
        var outsider = await AddUserAsync("outsider-1", "Out", "Sider");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        CurrentUser.UserId = outsider.Id;

        var inaccessible = await Assert.ThrowsAsync<NotFoundException>(() =>
            _projectHandler.Handle(new GetProjectQuery("project-1"), CancellationToken.None));

        // A genuinely missing ID throws the same exception type.
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _projectHandler.Handle(new GetProjectQuery("no-such-project"), CancellationToken.None));

        // The message echoes only the caller-supplied ID, so the unauthorized
        // response is byte-identical to the not-found response for that ID.
        Assert.Equal(new NotFoundException("Project", "project-1").Message, inaccessible.Message);
    }

    [Fact]
    public async Task GetProject_ReturnsProject_WhenUserIsMember()
    {
        var owner = await AddUserAsync("owner-1");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        CurrentUser.UserId = owner.Id;

        var result = await _projectHandler.Handle(new GetProjectQuery("project-1"), CancellationToken.None);

        Assert.Equal("project-1", result.Id);
    }
}
