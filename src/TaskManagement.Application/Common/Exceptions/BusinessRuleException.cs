namespace TaskManagement.Application.Common.Exceptions;

/// <summary>
/// Thrown when a request is well-formed but semantically unprocessable,
/// for example combining resources that belong to different projects.
/// Maps to HTTP 422 Unprocessable Entity.
/// </summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message)
        : base(message)
    {
    }
}
