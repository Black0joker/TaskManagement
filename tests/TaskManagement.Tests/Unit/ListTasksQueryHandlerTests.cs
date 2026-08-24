using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Features.Tasks.ListTasks;
using TaskManagement.Domain.Enums;
using TaskManagement.Tests.Unit.Testing;

namespace TaskManagement.Tests.Unit;

public class ListTasksQueryHandlerTests : HandlerTestBase
{
    private readonly ListTasksQueryHandler _handler;

    public ListTasksQueryHandlerTests()
    {
        _handler = new ListTasksQueryHandler(Context, ProjectAccess, CurrentUser);
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
    public async Task Handle_FiltersByStatus()
    {
        var userId = await SeedMemberWithProjectAsync();
        await AddTaskAsync("t-todo", "project-1", userId, status: TaskItemStatus.Todo);
        await AddTaskAsync("t-done", "project-1", userId, status: TaskItemStatus.Done);

        var result = await _handler.Handle(
            new ListTasksQuery("project-1", Status: TaskItemStatus.Done), CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("t-done", Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task Handle_FiltersByPriority()
    {
        var userId = await SeedMemberWithProjectAsync();
        await AddTaskAsync("t-low", "project-1", userId, priority: TaskItemPriority.Low);
        await AddTaskAsync("t-critical", "project-1", userId, priority: TaskItemPriority.Critical);

        var result = await _handler.Handle(
            new ListTasksQuery("project-1", Priority: TaskItemPriority.Critical), CancellationToken.None);

        Assert.Equal("t-critical", Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task Handle_FiltersByAssignee()
    {
        var userId = await SeedMemberWithProjectAsync();
        var other = await AddUserAsync("other-1", "Ot", "Her");
        await AddMemberAsync("project-1", other.Id, ProjectMemberRole.Member);
        await AddTaskAsync("t-mine", "project-1", userId, assignedToId: userId);
        await AddTaskAsync("t-theirs", "project-1", userId, assignedToId: other.Id);

        var result = await _handler.Handle(
            new ListTasksQuery("project-1", AssignedToId: other.Id), CancellationToken.None);

        Assert.Equal("t-theirs", Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task Handle_SearchesTitleAndDescription()
    {
        var userId = await SeedMemberWithProjectAsync();
        await AddTaskAsync("t-1", "project-1", userId, title: "Ship the release");
        await AddTaskAsync("t-2", "project-1", userId, title: "Other work", description: "includes release notes");
        await AddTaskAsync("t-3", "project-1", userId, title: "Unrelated");

        var result = await _handler.Handle(
            new ListTasksQuery("project-1", Search: "release"), CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.DoesNotContain(result.Items, t => t.Id == "t-3");
    }

    [Fact]
    public async Task Handle_SortsByPriorityDescending()
    {
        var userId = await SeedMemberWithProjectAsync();
        await AddTaskAsync("t-low", "project-1", userId, priority: TaskItemPriority.Low);
        await AddTaskAsync("t-critical", "project-1", userId, priority: TaskItemPriority.Critical);
        await AddTaskAsync("t-medium", "project-1", userId, priority: TaskItemPriority.Medium);

        var result = await _handler.Handle(
            new ListTasksQuery("project-1", SortBy: "priority", SortDirection: "desc"), CancellationToken.None);

        Assert.Equal(new[] { "t-critical", "t-medium", "t-low" }, result.Items.Select(t => t.Id).ToArray());
    }

    [Fact]
    public async Task Handle_DefaultsToNewestFirst()
    {
        var userId = await SeedMemberWithProjectAsync();
        var now = DateTime.UtcNow;
        await AddTaskAsync("t-old", "project-1", userId, createdAt: now.AddHours(-3));
        await AddTaskAsync("t-new", "project-1", userId, createdAt: now);
        await AddTaskAsync("t-mid", "project-1", userId, createdAt: now.AddHours(-1));

        var result = await _handler.Handle(new ListTasksQuery("project-1"), CancellationToken.None);

        Assert.Equal(new[] { "t-new", "t-mid", "t-old" }, result.Items.Select(t => t.Id).ToArray());
    }

    [Fact]
    public async Task Handle_PaginatesResults()
    {
        var userId = await SeedMemberWithProjectAsync();
        var now = DateTime.UtcNow;
        for (var i = 1; i <= 5; i++)
        {
            await AddTaskAsync($"t-{i}", "project-1", userId, createdAt: now.AddMinutes(i));
        }

        var page1 = await _handler.Handle(
            new ListTasksQuery("project-1", Page: 1, PageSize: 2), CancellationToken.None);
        var page3 = await _handler.Handle(
            new ListTasksQuery("project-1", Page: 3, PageSize: 2), CancellationToken.None);

        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(3, page1.TotalPages);
        Assert.Equal(2, page1.Items.Count);
        Assert.True(page1.HasNextPage);
        Assert.False(page1.HasPreviousPage);

        Assert.Single(page3.Items);
        Assert.False(page3.HasNextPage);
        Assert.True(page3.HasPreviousPage);
    }

    [Fact]
    public async Task Handle_HidesTasksFromProjectsUserIsNotMemberOf()
    {
        var memberId = await SeedMemberWithProjectAsync();
        var outsider = await AddUserAsync("outsider-1", "Out", "Sider");
        await AddProjectAsync("project-2", outsider.Id);
        await AddMemberAsync("project-2", outsider.Id, ProjectMemberRole.Owner);

        await AddTaskAsync("t-visible", "project-1", memberId);
        await AddTaskAsync("t-hidden", "project-2", outsider.Id);

        var result = await _handler.Handle(new ListTasksQuery(ProjectId: null), CancellationToken.None);

        Assert.Equal("t-visible", Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task Handle_ThrowsNotFound_WhenProjectFilterPointsAtMissingProject()
    {
        var memberId = await SeedMemberWithProjectAsync();
        _ = memberId;

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new ListTasksQuery("missing-project"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ThrowsNotFound_WhenUserCannotReadFilteredProject()
    {
        await SeedMemberWithProjectAsync();
        var outsider = await AddUserAsync("outsider-1", "Out", "Sider");
        await AddProjectAsync("project-2", outsider.Id);
        await AddMemberAsync("project-2", outsider.Id, ProjectMemberRole.Owner);
        CurrentUser.UserId = outsider.Id;

        // 404 instead of 403: an inaccessible project must be indistinguishable
        // from a missing one so project IDs cannot be enumerated.
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new ListTasksQuery("project-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SortsByTitle()
    {
        var userId = await SeedMemberWithProjectAsync();
        await AddTaskAsync("t-b", "project-1", userId, title: "Beta task");
        await AddTaskAsync("t-a", "project-1", userId, title: "Alpha task");
        await AddTaskAsync("t-c", "project-1", userId, title: "Gamma task");

        var ascending = await _handler.Handle(
            new ListTasksQuery("project-1", SortBy: "title"), CancellationToken.None);

        Assert.Equal(new[] { "t-a", "t-b", "t-c" }, ascending.Items.Select(t => t.Id).ToArray());

        var descending = await _handler.Handle(
            new ListTasksQuery("project-1", SortBy: "title", SortDirection: "desc"), CancellationToken.None);

        Assert.Equal(new[] { "t-c", "t-b", "t-a" }, descending.Items.Select(t => t.Id).ToArray());
    }

    [Fact]
    public async Task Handle_SortsByStatus_FollowsWorkflowOrder()
    {
        var userId = await SeedMemberWithProjectAsync();
        await AddTaskAsync("t-done", "project-1", userId, status: TaskItemStatus.Done);
        await AddTaskAsync("t-todo", "project-1", userId, status: TaskItemStatus.Todo);
        await AddTaskAsync("t-cancelled", "project-1", userId, status: TaskItemStatus.Cancelled);
        await AddTaskAsync("t-review", "project-1", userId, status: TaskItemStatus.InReview);
        await AddTaskAsync("t-progress", "project-1", userId, status: TaskItemStatus.InProgress);

        var ascending = await _handler.Handle(
            new ListTasksQuery("project-1", SortBy: "status"), CancellationToken.None);

        Assert.Equal(
            new[] { "t-todo", "t-progress", "t-review", "t-done", "t-cancelled" },
            ascending.Items.Select(t => t.Id).ToArray());

        var descending = await _handler.Handle(
            new ListTasksQuery("project-1", SortBy: "status", SortDirection: "desc"), CancellationToken.None);

        Assert.Equal(
            new[] { "t-cancelled", "t-done", "t-review", "t-progress", "t-todo" },
            descending.Items.Select(t => t.Id).ToArray());
    }

    [Fact]
    public async Task Handle_SortsByPriorityAscending_FollowsPriorityLevels()
    {
        var userId = await SeedMemberWithProjectAsync();
        await AddTaskAsync("t-low", "project-1", userId, priority: TaskItemPriority.Low);
        await AddTaskAsync("t-critical", "project-1", userId, priority: TaskItemPriority.Critical);
        await AddTaskAsync("t-medium", "project-1", userId, priority: TaskItemPriority.Medium);
        await AddTaskAsync("t-high", "project-1", userId, priority: TaskItemPriority.High);

        var ascending = await _handler.Handle(
            new ListTasksQuery("project-1", SortBy: "priority"), CancellationToken.None);

        Assert.Equal(
            new[] { "t-low", "t-medium", "t-high", "t-critical" },
            ascending.Items.Select(t => t.Id).ToArray());
    }
}
