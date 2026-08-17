using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities;

public class ProjectMember
{
    public string ProjectId { get; set; } = string.Empty;
    public Project Project { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    public ProjectMemberRole Role { get; set; } = ProjectMemberRole.Member;
}
