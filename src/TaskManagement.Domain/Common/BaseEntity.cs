namespace TaskManagement.Domain.Common;

public abstract class BaseEntity : IVersioned
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int Version { get; set; }
}
