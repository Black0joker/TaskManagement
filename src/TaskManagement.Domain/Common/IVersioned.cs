namespace TaskManagement.Domain.Common;

/// <summary>
/// Marks an entity that participates in optimistic concurrency control.
/// <see cref="Version"/> is bumped every time a modified entity is saved and
/// is used as the EF Core concurrency token: a stale write affects no rows and
/// surfaces as <c>DbUpdateConcurrencyException</c> (HTTP 409) instead of
/// silently overwriting newer data.
/// </summary>
public interface IVersioned
{
    int Version { get; set; }
}
