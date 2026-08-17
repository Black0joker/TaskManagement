using FluentValidation.Results;

namespace TaskManagement.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public ValidationException()
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(e => ToCamelCase(e.PropertyName), e => e.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }

    private static string ToCamelCase(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName) || char.IsLower(propertyName[0]))
        {
            return propertyName;
        }

        return char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
    }

    public IDictionary<string, string[]> Errors { get; }
}
