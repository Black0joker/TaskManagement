using TaskManagement.Domain.Common;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities;

public class TaskItem : BaseAuditableEntity
{
    public string ProjectId { get; set; } = string.Empty;
    public Project Project { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;
    public TaskItemPriority Priority { get; set; } = TaskItemPriority.Medium;

    public DateTime? DueDate { get; set; }

    public string? AssignedToId { get; set; }
    public User? AssignedTo { get; set; }

    public string CreatedById { get; set; } = string.Empty;
    public User CreatedBy { get; set; } = null!;

    // Navigation properties
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<TaskItemLabel> TaskItemLabels { get; set; } = new List<TaskItemLabel>();
}
