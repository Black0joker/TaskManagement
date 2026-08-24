using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Projects;

namespace TaskManagement.Tests.Unit.Testing;

/// <summary>
/// Shared fixture for application-layer handler tests. Runs the real
/// <see cref="AppDbContext"/> against EF Core InMemory and the real
/// <see cref="ProjectAccessService"/> against seeded membership rows.
/// </summary>
public abstract class HandlerTestBase : IDisposable
{
    private readonly string _databaseName = $"unit-{Guid.NewGuid():N}";

    protected HandlerTestBase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options;

        Context = new AppDbContext(options);
        CurrentUser = new StubCurrentUserService();
        ProjectAccess = new ProjectAccessService(Context, CurrentUser);
    }

    protected AppDbContext Context { get; }
    protected StubCurrentUserService CurrentUser { get; }
    protected ProjectAccessService ProjectAccess { get; }

    /// <summary>
    /// Creates a second context over the same in-memory database so tests can
    /// simulate concurrent writers for optimistic-concurrency scenarios.
    /// </summary>
    protected AppDbContext CreateParallelContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options);

    protected async Task<User> AddUserAsync(string id, string firstName = "Test", string lastName = "User")
    {
        var user = new User
        {
            Id = id,
            UserName = $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}.{Guid.NewGuid():N}@test.local",
            Email = $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}.{Guid.NewGuid():N}@test.local",
            FirstName = firstName,
            LastName = lastName
        };

        Context.Users.Add(user);
        await Context.SaveChangesAsync();
        return user;
    }

    protected async Task<Project> AddProjectAsync(string id, string createdById)
    {
        var project = new Project
        {
            Id = id,
            Name = $"Project {id}",
            CreatedById = createdById
        };

        Context.Projects.Add(project);
        await Context.SaveChangesAsync();
        return project;
    }

    protected async Task AddMemberAsync(string projectId, string userId, ProjectMemberRole role)
    {
        Context.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = projectId,
            UserId = userId,
            Role = role
        });
        await Context.SaveChangesAsync();
    }

    protected async Task<TaskItem> AddTaskAsync(
        string id,
        string projectId,
        string createdById,
        string title = "Task",
        string? description = null,
        TaskItemStatus status = TaskItemStatus.Todo,
        TaskItemPriority priority = TaskItemPriority.Medium,
        string? assignedToId = null,
        DateTime? dueDate = null,
        DateTime? createdAt = null)
    {
        var task = new TaskItem
        {
            Id = id,
            ProjectId = projectId,
            Title = title,
            Description = description,
            Status = status,
            Priority = priority,
            AssignedToId = assignedToId,
            DueDate = dueDate,
            CreatedById = createdById
        };

        Context.TaskItems.Add(task);
        await Context.SaveChangesAsync();

        if (createdAt is not null)
        {
            task.CreatedAt = createdAt.Value;
            task.UpdatedAt = createdAt.Value;
            await Context.SaveChangesAsync();
        }

        return task;
    }

    public void Dispose() => Context.Dispose();
}
