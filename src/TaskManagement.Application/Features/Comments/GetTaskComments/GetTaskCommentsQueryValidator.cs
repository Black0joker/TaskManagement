using FluentValidation;

namespace TaskManagement.Application.Features.Comments.GetTaskComments;

public class GetTaskCommentsQueryValidator : AbstractValidator<GetTaskCommentsQuery>
{
    public GetTaskCommentsQueryValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
    }
}
