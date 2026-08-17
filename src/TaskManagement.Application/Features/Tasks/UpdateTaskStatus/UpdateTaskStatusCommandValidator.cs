using FluentValidation;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Features.Tasks.UpdateTaskStatus;

public class UpdateTaskStatusCommandValidator : AbstractValidator<UpdateTaskStatusCommand>
{
    public UpdateTaskStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Status)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("'Status' is required.")
            .Must(s => Enum.IsDefined(typeof(TaskItemStatus), s!.Value))
            .WithMessage("'Status' is not a valid status value.");
    }
}
