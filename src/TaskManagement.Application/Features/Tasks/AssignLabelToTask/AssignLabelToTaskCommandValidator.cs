using FluentValidation;

namespace TaskManagement.Application.Features.Tasks.AssignLabelToTask;

public class AssignLabelToTaskCommandValidator : AbstractValidator<AssignLabelToTaskCommand>
{
    public AssignLabelToTaskCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.LabelId).NotEmpty();
    }
}
