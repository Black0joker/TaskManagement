using FluentValidation;

namespace TaskManagement.Application.Features.Labels.CreateProjectLabel;

public class CreateProjectLabelCommandValidator : AbstractValidator<CreateProjectLabelCommand>
{
    public CreateProjectLabelCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Color).NotEmpty().Matches("^#[0-9A-Fa-f]{6}$");
    }
}
