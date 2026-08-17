using FluentValidation;

namespace TaskManagement.Application.Features.ProjectMembers.UpdateProjectMemberRole;

public class UpdateProjectMemberRoleCommandValidator : AbstractValidator<UpdateProjectMemberRoleCommand>
{
    public UpdateProjectMemberRoleCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role).IsInEnum();
    }
}
