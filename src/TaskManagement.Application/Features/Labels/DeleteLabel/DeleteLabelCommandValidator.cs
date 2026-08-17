using FluentValidation;

namespace TaskManagement.Application.Features.Labels.DeleteLabel;

public class DeleteLabelCommandValidator : AbstractValidator<DeleteLabelCommand>
{
    public DeleteLabelCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
