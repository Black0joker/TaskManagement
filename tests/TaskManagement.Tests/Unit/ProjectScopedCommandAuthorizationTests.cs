using Microsoft.Extensions.Logging.Abstractions;
using TaskManagement.Application.Abstractions.Identity;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Features.Comments.CreateComment;
using TaskManagement.Application.Features.Comments.DeleteComment;
using TaskManagement.Application.Features.Comments.UpdateComment;
using TaskManagement.Application.Features.Labels.CreateProjectLabel;
using TaskManagement.Application.Features.Labels.DeleteLabel;
using TaskManagement.Application.Features.Labels.UpdateLabel;
using TaskManagement.Application.Features.ProjectMembers.AddProjectMember;
using TaskManagement.Application.Features.ProjectMembers.RemoveProjectMember;
using TaskManagement.Application.Features.ProjectMembers.UpdateProjectMemberRole;
using TaskManagement.Application.Features.Projects.DeleteProject;
using TaskManagement.Application.Features.Projects.UpdateProject;
using TaskManagement.Application.Features.Tasks.AssignLabelToTask;
using TaskManagement.Application.Features.Tasks.CreateTask;
using TaskManagement.Application.Features.Tasks.DeleteTask;
using TaskManagement.Application.Features.Tasks.RemoveLabelFromTask;
using TaskManagement.Application.Features.Tasks.UpdateTask;
using TaskManagement.Application.Features.Tasks.UpdateTaskAssignee;
using TaskManagement.Application.Features.Tasks.UpdateTaskDueDate;
using TaskManagement.Application.Features.Tasks.UpdateTaskPriority;
using TaskManagement.Application.Features.Tasks.UpdateTaskStatus;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Tests.Unit.Testing;

namespace TaskManagement.Tests.Unit;

/// <summary>
/// Behavioral enforcement of the authorization convention: a user who is not
/// a member of a project must receive ForbiddenAccessException from every
/// project-scoped mutation command, regardless of controller policies.
/// When adding a new project-scoped command, add its outsider case here.
/// </summary>
public class ProjectScopedCommandAuthorizationTests : HandlerTestBase
{
    private sealed record SeededData(
        string OwnerId,
        string OutsiderId,
        string ProjectId,
        string TaskId,
        string LabelId,
        string CommentId);

    private async Task<SeededData> SeedProjectScopedDataAsync()
    {
        var owner = await AddUserAsync("owner-1", "Own", "Er");
        var outsider = await AddUserAsync("outsider-1", "Out", "Sider");

        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);

        var task = await AddTaskAsync("task-1", "project-1", owner.Id, dueDate: DateTime.UtcNow.AddDays(3));

        var label = new Label { Id = "label-1", Name = "Bug", Color = "#EF4444", ProjectId = "project-1" };
        Context.Labels.Add(label);

        var comment = new Comment { Id = "comment-1", TaskItemId = task.Id, AuthorId = owner.Id, Content = "Original" };
        Context.Comments.Add(comment);

        await Context.SaveChangesAsync();

        CurrentUser.UserId = outsider.Id;

        return new SeededData(owner.Id, outsider.Id, "project-1", task.Id, label.Id, comment.Id);
    }

    // ---- tasks ----

    [Fact]
    public async Task CreateTask_ByNonMember_ThrowsForbidden()
    {
        var data = await SeedProjectScopedDataAsync();
        var handler = new CreateTaskCommandHandler(
            Context, ProjectAccess, CurrentUser, NullLogger<CreateTaskCommandHandler>.Instance);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new CreateTaskCommand(data.ProjectId, "New", null, TaskItemStatus.Todo,
                TaskItemPriority.Medium, null, DateTime.UtcNow.AddDays(1)),
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateTask_ByNonMember_ThrowsForbidden()
    {
        var data = await SeedProjectScopedDataAsync();
        var handler = new UpdateTaskCommandHandler(Context, ProjectAccess);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new UpdateTaskCommand(data.TaskId, "Edited", null, TaskItemStatus.Todo,
                TaskItemPriority.Medium, null, DateTime.UtcNow.AddDays(1)),
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateTaskStatus_ByNonMember_ThrowsForbidden()
    {
        var data = await SeedProjectScopedDataAsync();
        var handler = new UpdateTaskStatusCommandHandler(
            Context, ProjectAccess, CurrentUser, NullLogger<UpdateTaskStatusCommandHandler>.Instance);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new UpdateTaskStatusCommand(data.TaskId, TaskItemStatus.InProgress),
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateTaskPriority_ByNonMember_ThrowsForbidden()
    {
        var data = await SeedProjectScopedDataAsync();
        var handler = new UpdateTaskPriorityCommandHandler(Context, ProjectAccess);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new UpdateTaskPriorityCommand(data.TaskId, TaskItemPriority.High),
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateTaskAssignee_ByNonMember_ThrowsForbidden()
    {
        var data = await SeedProjectScopedDataAsync();
        var handler = new UpdateTaskAssigneeCommandHandler(
            Context, ProjectAccess, CurrentUser, NullLogger<UpdateTaskAssigneeCommandHandler>.Instance);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new UpdateTaskAssigneeCommand(data.TaskId, data.OwnerId),
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateTaskDueDate_ByNonMember_ThrowsForbidden()
    {
        var data = await SeedProjectScopedDataAsync();
        var handler = new UpdateTaskDueDateCommandHandler(Context, ProjectAccess);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new UpdateTaskDueDateCommand(data.TaskId, DateTime.UtcNow.AddDays(2)),
            CancellationToken.None));
    }

    [Fact]
    public async Task DeleteTask_ByNonMember_ThrowsForbidden()
    {
        var data = await SeedProjectScopedDataAsync();
        var handler = new DeleteTaskCommandHandler(Context, ProjectAccess);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new DeleteTaskCommand(data.TaskId), CancellationToken.None));
    }

    [Fact]
    public async Task AssignLabelToTask_ByNonMember_ThrowsForbidden()
    {
        var data = await SeedProjectScopedDataAsync();
        var handler = new AssignLabelToTaskCommandHandler(Context, ProjectAccess);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new AssignLabelToTaskCommand(data.TaskId, data.LabelId), CancellationToken.None));
    }

    [Fact]
    public async Task RemoveLabelFromTask_ByNonMember_ThrowsForbidden()
    {
        var data = await SeedProjectScopedDataAsync();
        var handler = new RemoveLabelFromTaskCommandHandler(Context, ProjectAccess);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new RemoveLabelFromTaskCommand(data.TaskId, data.LabelId), CancellationToken.None));
    }

    // ---- comments ----

    [Fact]
    public async Task CreateComment_ByNonMember_ThrowsForbidden()
    {
        var data = await SeedProjectScopedDataAsync();
        var handler = new CreateCommentCommandHandler(
            Context, ProjectAccess, CurrentUser, NullLogger<CreateCommentCommandHandler>.Instance);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new CreateCommentCommand(data.TaskId, "Intruder comment"), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateComment_ByNonAuthorNonMember_ThrowsForbidden()
    {
        var data = await SeedProjectScopedDataAsync();
        var handler = new UpdateCommentCommandHandler(Context, ProjectAccess, CurrentUser);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new UpdateCommentCommand(data.CommentId, "Rewritten"), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteComment_ByNonAuthorNonMember_ThrowsForbidden()
    {
        var data = await SeedProjectScopedDataAsync();
        var handler = new DeleteCommentCommandHandler(Context, ProjectAccess, CurrentUser);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new DeleteCommentCommand(data.CommentId), CancellationToken.None));
    }

    // ---- labels ----

    [Fact]
    public async Task CreateProjectLabel_ByNonMember_ThrowsForbidden()
    {
        var data = await SeedProjectScopedDataAsync();
        var handler = new CreateProjectLabelCommandHandler(Context, ProjectAccess);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new CreateProjectLabelCommand(data.ProjectId, "Intruder", "#00FF00"),
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateLabel_ByNonMember_ThrowsForbidden()
    {
        var data = await SeedProjectScopedDataAsync();
        var handler = new UpdateLabelCommandHandler(Context, ProjectAccess);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new UpdateLabelCommand(data.LabelId, "Renamed", "#0000FF"), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteLabel_ByNonMember_ThrowsForbidden()
    {
        var data = await SeedProjectScopedDataAsync();
        var handler = new DeleteLabelCommandHandler(Context, ProjectAccess);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new DeleteLabelCommand(data.LabelId), CancellationToken.None));
    }

    // ---- members ----

    [Fact]
    public async Task AddProjectMember_ByNonMember_ThrowsForbidden()
    {
        var data = await SeedProjectScopedDataAsync();
        var handler = new AddProjectMemberCommandHandler(Context, ProjectAccess, new UnreachableIdentityService());

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new AddProjectMemberCommand(data.ProjectId, data.OutsiderId, ProjectMemberRole.Member),
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateProjectMemberRole_ByNonMember_ThrowsForbidden()
    {
        var data = await SeedProjectScopedDataAsync();
        var handler = new UpdateProjectMemberRoleCommandHandler(Context, ProjectAccess);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new UpdateProjectMemberRoleCommand(data.ProjectId, data.OwnerId, ProjectMemberRole.Member),
            CancellationToken.None));
    }

    [Fact]
    public async Task RemoveProjectMember_ByNonMember_ThrowsForbidden()
    {
        var data = await SeedProjectScopedDataAsync();
        var handler = new RemoveProjectMemberCommandHandler(Context, ProjectAccess, CurrentUser);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new RemoveProjectMemberCommand(data.ProjectId, data.OwnerId), CancellationToken.None));
    }

    // ---- projects ----

    [Fact]
    public async Task UpdateProject_ByNonMember_ThrowsForbidden()
    {
        var data = await SeedProjectScopedDataAsync();
        var handler = new UpdateProjectCommandHandler(Context, ProjectAccess);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new UpdateProjectCommand(data.ProjectId, "Hijacked", null), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteProject_ByNonMember_ThrowsForbidden()
    {
        var data = await SeedProjectScopedDataAsync();
        var handler = new DeleteProjectCommandHandler(Context, ProjectAccess);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new DeleteProjectCommand(data.ProjectId), CancellationToken.None));
    }

    /// <summary>
    /// The authorization check must fire before any identity lookup; every
    /// method throwing proves the forbidden path never touches identity.
    /// </summary>
    private sealed class UnreachableIdentityService : IIdentityService
    {
        public Task<ApplicationUserDto?> GetUserByEmailAsync(string email) =>
            throw new InvalidOperationException("Authorization must reject before the identity lookup.");

        public Task<ApplicationUserDto?> GetUserByIdAsync(string userId) =>
            throw new InvalidOperationException("Authorization must reject before the identity lookup.");

        public Task<IdentityOperationResult> CreateUserAsync(CreateApplicationUserRequest request, string password) =>
            throw new InvalidOperationException("Authorization must reject before the identity lookup.");

        public Task<CredentialValidationResult> ValidateCredentialsAsync(string email, string password) =>
            throw new InvalidOperationException("Authorization must reject before the identity lookup.");

        public Task<IReadOnlyList<string>> GetRolesAsync(string userId) =>
            throw new InvalidOperationException("Authorization must reject before the identity lookup.");
    }
}
