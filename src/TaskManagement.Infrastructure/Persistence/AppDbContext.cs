using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<User>, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<TaskItemLabel> TaskItemLabels => Set<TaskItemLabel>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Optimistic concurrency: every versioned entity carries a token that
        // EF includes in the WHERE clause of UPDATE/DELETE statements. Stale
        // writes affect no rows and surface as DbUpdateConcurrencyException
        // (mapped to HTTP 409) instead of silently overwriting newer data.
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(IVersioned).IsAssignableFrom(entityType.ClrType))
            {
                var versionProperty = entityType.FindProperty(nameof(IVersioned.Version));
                if (versionProperty is not null)
                {
                    versionProperty.IsConcurrencyToken = true;
                }
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Bump the optimistic-concurrency token on every modified entity so a
        // concurrent writer holding the old token fails instead of being
        // silently overwritten.
        foreach (var entry in ChangeTracker.Entries<IVersioned>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.Version++;
            }
        }

        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
