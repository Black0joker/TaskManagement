using FluentValidation;

namespace TaskManagement.Application.Features.Tasks.RemoveLabelFromTask;

public class RemoveLabelFromTaskCommandValidator : AbstractValidator<RemoveLabelFromTaskCommand>
{
    public RemoveLabelFromTaskCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.LabelId).NotEmpty();
    }
}
