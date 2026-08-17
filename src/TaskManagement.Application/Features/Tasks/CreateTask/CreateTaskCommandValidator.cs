using FluentValidation;

namespace TaskManagement.Application.Features.Tasks.CreateTask;

public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Priority).IsInEnum();

        RuleFor(x => x.DueDate)
            .Must(dueDate => dueDate is null || dueDate.Value >= DateTime.UtcNow.Date)
            .WithMessage("Due date cannot be in the past.");
    }
}
