namespace TaskManagement.Application.Common.Exceptions;

/// <summary>
/// Thrown when a request conflicts with the current state of a resource,
/// for example creating a duplicate label or adding a member twice.
/// Maps to HTTP 409 Conflict.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(string name, object key)
        : base($"{name} \"{key}\" already exists.")
    {
    }
}
