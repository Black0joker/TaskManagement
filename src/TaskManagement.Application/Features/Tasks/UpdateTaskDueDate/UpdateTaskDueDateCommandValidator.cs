using FluentValidation;

namespace TaskManagement.Application.Features.Tasks.UpdateTaskDueDate;

public class UpdateTaskDueDateCommandValidator : AbstractValidator<UpdateTaskDueDateCommand>
{
    public UpdateTaskDueDateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.DueDate)
            .Must(dueDate => dueDate is null || dueDate.Value >= DateTime.UtcNow.Date)
            .WithMessage("Due date cannot be in the past.");
    }
}
