using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Entities;

public class Label : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;

    public string ProjectId { get; set; } = string.Empty;
    public Project Project { get; set; } = null!;

    // Navigation properties
    public ICollection<TaskItemLabel> TaskItemLabels { get; set; } = new List<TaskItemLabel>();
}
