using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Entities;

public class Comment : BaseAuditableEntity
{
    public string TaskItemId { get; set; } = string.Empty;
    public TaskItem TaskItem { get; set; } = null!;

    public string AuthorId { get; set; } = string.Empty;
    public User Author { get; set; } = null!;

    public string Content { get; set; } = string.Empty;
}
