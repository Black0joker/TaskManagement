using FluentValidation;

namespace TaskManagement.Application.Features.Tasks.ListTasks;

public class ListTasksQueryValidator : AbstractValidator<ListTasksQuery>
{
    public static readonly string[] SortableProperties = ["createdAt", "dueDate", "priority"];
    private static readonly string[] SortDirections = ["asc", "desc"];

    public ListTasksQueryValidator()
    {
        RuleFor(x => x.SortBy)
            .Must(sortBy => SortableProperties.Contains(sortBy!, StringComparer.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy))
            .WithMessage($"sortBy must be one of: {string.Join(", ", SortableProperties)}.");

        RuleFor(x => x.SortDirection)
            .Must(direction => SortDirections.Contains(direction!, StringComparer.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrWhiteSpace(x.SortDirection))
            .WithMessage("sortDirection must be 'asc' or 'desc'.");
    }
}
