using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Abstractions.Persistence;

public interface IApplicationDbContext
{
    DbSet<Project> Projects { get; }
    DbSet<TaskItem> TaskItems { get; }
    DbSet<Comment> Comments { get; }
    DbSet<Label> Labels { get; }
    DbSet<TaskItemLabel> TaskItemLabels { get; }
    DbSet<ProjectMember> ProjectMembers { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<User> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
