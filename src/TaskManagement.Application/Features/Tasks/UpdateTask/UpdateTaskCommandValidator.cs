using FluentValidation;

namespace TaskManagement.Application.Features.Tasks.UpdateTask;

public class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Priority).IsInEnum();

        RuleFor(x => x.DueDate)
            .Must(dueDate => dueDate is null || dueDate.Value >= DateTime.UtcNow.Date)
            .WithMessage("Due date cannot be in the past.");
    }
}
