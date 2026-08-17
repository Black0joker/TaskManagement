namespace TaskManagement.Domain.Entities;

public class TaskItemLabel
{
    public string TaskItemId { get; set; } = string.Empty;
    public TaskItem TaskItem { get; set; } = null!;

    public string LabelId { get; set; } = string.Empty;
    public Label Label { get; set; } = null!;
}
