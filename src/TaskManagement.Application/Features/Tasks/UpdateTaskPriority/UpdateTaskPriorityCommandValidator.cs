using FluentValidation;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Features.Tasks.UpdateTaskPriority;

public class UpdateTaskPriorityCommandValidator : AbstractValidator<UpdateTaskPriorityCommand>
{
    public UpdateTaskPriorityCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Priority)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("'Priority' is required.")
            .Must(p => Enum.IsDefined(typeof(TaskItemPriority), p!.Value))
            .WithMessage("'Priority' is not a valid priority value.");
    }
}
