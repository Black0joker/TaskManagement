using TaskManagement.Domain.Common;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities;

public class ProjectMember : IVersioned
{
    public string ProjectId { get; set; } = string.Empty;
    public Project Project { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    public ProjectMemberRole Role { get; set; } = ProjectMemberRole.Member;

    public int Version { get; set; }
}
