using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Entities;

public class Project : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public string CreatedById { get; set; } = string.Empty;
    public User CreatedBy { get; set; } = null!;

    // Navigation properties
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<Label> Labels { get; set; } = new List<Label>();
}
