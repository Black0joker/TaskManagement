using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Tests.Unit.Testing;

namespace TaskManagement.Tests.Unit;

/// <summary>
/// Verifies the optimistic-concurrency mechanism (IVersioned token): the
/// version is bumped on every saved modification and stale writes are
/// rejected when the row changed since it was read.
/// </summary>
public class OptimisticConcurrencyTests : HandlerTestBase
{
    [Fact]
    public async Task SaveChanges_BumpsVersion_OnEveryModification()
    {
        var task = await SeedTaskAsync();

        Assert.Equal(0, task.Version);

        task.Title = "First edit";
        await Context.SaveChangesAsync();
        Assert.Equal(1, task.Version);

        task.Title = "Second edit";
        await Context.SaveChangesAsync();
        Assert.Equal(2, task.Version);
    }

    [Fact]
    public async Task ConcurrentUpdate_ToSameTask_ThrowsConcurrencyException()
    {
        var task = await SeedTaskAsync();

        // A concurrent writer commits through a parallel context first.
        using (var parallel = CreateParallelContext())
        {
            var concurrent = await parallel.TaskItems.SingleAsync(t => t.Id == task.Id);
            concurrent.Title = "Writer B wins";
            await parallel.SaveChangesAsync();
        }

        // This context still holds the stale version token: its write must
        // fail instead of silently overwriting the concurrent change.
        task.Title = "Writer A";

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => Context.SaveChangesAsync());
    }

    [Fact]
    public async Task ConcurrentUpdate_ToSameProjectMember_ThrowsConcurrencyException()
    {
        var owner = await AddUserAsync("owner-1", "Own", "Er");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);

        using (var parallel = CreateParallelContext())
        {
            var concurrent = await parallel.ProjectMembers.SingleAsync(
                pm => pm.ProjectId == "project-1" && pm.UserId == owner.Id);
            concurrent.Role = ProjectMemberRole.Member;
            await parallel.SaveChangesAsync();
        }

        var member = await Context.ProjectMembers.SingleAsync(
            pm => pm.ProjectId == "project-1" && pm.UserId == owner.Id);

        member.Role = ProjectMemberRole.Admin;

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => Context.SaveChangesAsync());
    }

    private async Task<TaskItem> SeedTaskAsync()
    {
        var owner = await AddUserAsync("owner-1", "Own", "Er");
        await AddProjectAsync("project-1", owner.Id);
        await AddMemberAsync("project-1", owner.Id, ProjectMemberRole.Owner);
        return await AddTaskAsync("task-1", "project-1", owner.Id);
    }
}
