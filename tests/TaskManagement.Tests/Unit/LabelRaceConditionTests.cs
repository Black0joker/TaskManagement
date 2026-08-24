using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Features.Labels.CreateProjectLabel;
using TaskManagement.Application.Features.Labels.UpdateLabel;
using TaskManagement.Application.Features.Projects;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Projects;
using TaskManagement.Tests.Unit.Testing;

namespace TaskManagement.Tests.Unit;

/// <summary>
/// Simulates the database rejecting a label save with a <see cref="DbUpdateException"/>,
/// as happens when a concurrent request wins the race and the unique index on
/// (Name, ProjectId) fires. The EF Core InMemory provider does not enforce unique
/// indexes, so the failure is injected via a context subclass.
/// </summary>
public class LabelRaceConditionTests : IDisposable
{
    private readonly string _databaseName = $"race-{Guid.NewGuid():N}";
    private readonly AppDbContext _seedContext;
    private readonly ThrowingSaveChangesContext _throwingContext;
    private readonly StubCurrentUserService _currentUser;

    public LabelRaceConditionTests()
    {
        var seedOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options;

        _seedContext = new AppDbContext(seedOptions);
        _throwingContext = new ThrowingSaveChangesContext(seedOptions);
        _currentUser = new StubCurrentUserService();
    }

    [Fact]
    public async Task CreateProjectLabel_ReturnsConflict_WhenDatabaseRejectsDuplicate()
    {
        var owner = await SeedProjectAsync("project-1", "owner-1");
        _currentUser.UserId = owner;
        _throwingContext.FailNextSave = true;

        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new CreateProjectLabelCommand("project-1", "Backend", "#3B82F6"),
            CancellationToken.None));

        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task CreateProjectLabel_Succeeds_WhenNoConflict()
    {
        var owner = await SeedProjectAsync("project-1", "owner-1");
        _currentUser.UserId = owner;

        var handler = CreateHandler();

        var result = await handler.Handle(
            new CreateProjectLabelCommand("project-1", "Backend", "#3B82F6"),
            CancellationToken.None);

        Assert.Equal("Backend", result.Name);
        Assert.Equal("#3B82F6", result.Color);
    }

    [Fact]
    public async Task CreateProjectLabel_ReturnsConflict_WhenDuplicateExists()
    {
        var owner = await SeedProjectAsync("project-1", "owner-1");
        _currentUser.UserId = owner;

        _seedContext.Labels.Add(new Label
        {
            ProjectId = "project-1",
            Name = "Backend",
            Color = "#3B82F6"
        });
        await _seedContext.SaveChangesAsync();

        var handler = CreateHandler();

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new CreateProjectLabelCommand("project-1", "Backend", "#EF4444"),
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateLabel_ReturnsConflict_WhenDatabaseRejectsRename()
    {
        var owner = await SeedProjectAsync("project-1", "owner-1");
        _currentUser.UserId = owner;

        _seedContext.Labels.Add(new Label
        {
            Id = "label-1",
            ProjectId = "project-1",
            Name = "Backend",
            Color = "#3B82F6"
        });
        await _seedContext.SaveChangesAsync();

        _throwingContext.FailNextSave = true;
        var handler = CreateUpdateHandler();

        var ex = await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new UpdateLabelCommand("label-1", "Backend", "#000000"),
            CancellationToken.None));

        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task UpdateLabel_ReturnsConflict_WhenRenamingToExistingName()
    {
        var owner = await SeedProjectAsync("project-1", "owner-1");
        _currentUser.UserId = owner;

        _seedContext.Labels.AddRange(
            new Label { Id = "label-1", ProjectId = "project-1", Name = "Backend", Color = "#3B82F6" },
            new Label { Id = "label-2", ProjectId = "project-1", Name = "Frontend", Color = "#10B981" });
        await _seedContext.SaveChangesAsync();

        var handler = CreateUpdateHandler();

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new UpdateLabelCommand("label-2", "Backend", "#10B981"),
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateLabel_Succeeds_WhenNoConflict()
    {
        var owner = await SeedProjectAsync("project-1", "owner-1");
        _currentUser.UserId = owner;

        _seedContext.Labels.Add(new Label
        {
            Id = "label-1",
            ProjectId = "project-1",
            Name = "Backend",
            Color = "#3B82F6"
        });
        await _seedContext.SaveChangesAsync();

        var handler = CreateUpdateHandler();

        var result = await handler.Handle(
            new UpdateLabelCommand("label-1", "API", "#8B5CF6"),
            CancellationToken.None);

        Assert.Equal("API", result.Name);
        Assert.Equal("#8B5CF6", result.Color);
    }

    private CreateProjectLabelCommandHandler CreateHandler()
    {
        var projectAccess = new ProjectAccessService(_throwingContext, _currentUser);
        return new CreateProjectLabelCommandHandler(_throwingContext, projectAccess);
    }

    private UpdateLabelCommandHandler CreateUpdateHandler()
    {
        var projectAccess = new ProjectAccessService(_throwingContext, _currentUser);
        return new UpdateLabelCommandHandler(_throwingContext, projectAccess);
    }

    private async Task<string> SeedProjectAsync(string projectId, string ownerId)
    {
        var user = new User
        {
            Id = ownerId,
            UserName = $"{ownerId}@test.local",
            Email = $"{ownerId}@test.local",
            FirstName = "Test",
            LastName = "Owner"
        };

        _seedContext.Users.Add(user);
        _seedContext.Projects.Add(new Project
        {
            Id = projectId,
            Name = $"Project {projectId}",
            CreatedById = ownerId
        });
        _seedContext.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = projectId,
            UserId = ownerId,
            Role = ProjectMemberRole.Owner
        });

        await _seedContext.SaveChangesAsync();
        return ownerId;
    }

    public void Dispose()
    {
        _seedContext.Dispose();
        _throwingContext.Dispose();
    }

    /// <summary>
    /// Shares the seeded InMemory database but fails the next SaveChangesAsync
    /// with a <see cref="DbUpdateException"/> when <see cref="FailNextSave"/> is set,
    /// emulating a unique-index violation raised by the database.
    /// </summary>
    private sealed class ThrowingSaveChangesContext : AppDbContext
    {
        public ThrowingSaveChangesContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public bool FailNextSave { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (FailNextSave)
            {
                FailNextSave = false;
                throw new DbUpdateException(
                    "Cannot insert duplicate key row in object 'dbo.Labels' with unique index 'IX_Labels_Name_ProjectId'.");
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
