using FluentValidation;

namespace TaskManagement.Application.Features.Tasks.UpdateTaskAssignee;

public class UpdateTaskAssigneeCommandValidator : AbstractValidator<UpdateTaskAssigneeCommand>
{
    public UpdateTaskAssigneeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        // UserId may be null or empty: that unassigns the task.
    }
}
