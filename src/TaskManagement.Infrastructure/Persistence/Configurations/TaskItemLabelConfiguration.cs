using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations;

public class TaskItemLabelConfiguration : IEntityTypeConfiguration<TaskItemLabel>
{
    public void Configure(EntityTypeBuilder<TaskItemLabel> builder)
    {
        builder.HasKey(t => new { t.TaskItemId, t.LabelId });

        builder.HasOne(t => t.TaskItem)
            .WithMany(t => t.TaskItemLabels)
            .HasForeignKey(t => t.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Label)
            .WithMany(l => l.TaskItemLabels)
            .HasForeignKey(t => t.LabelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
