using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Exceptions;

namespace TaskManagement.Application.Features.ProjectMembers.GetProjectMembers;

public class GetProjectMembersQueryHandler : IRequestHandler<GetProjectMembersQuery, IReadOnlyList<ProjectMemberResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IProjectAccessService _projectAccess;

    public GetProjectMembersQueryHandler(
        IApplicationDbContext context,
        IProjectAccessService projectAccess)
    {
        _context = context;
        _projectAccess = projectAccess;
    }

    public async Task<IReadOnlyList<ProjectMemberResponse>> Handle(GetProjectMembersQuery request, CancellationToken cancellationToken)
    {
        var projectExists = await _context.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.ProjectId, cancellationToken);

        // The same 404 is returned whether the project does not exist or the
        // user cannot read it, so a project ID's existence cannot be probed.
        if (!projectExists || !await _projectAccess.CanReadAsync(request.ProjectId, cancellationToken))
        {
            throw new NotFoundException("Project", request.ProjectId);
        }

        return await _context.ProjectMembers
            .AsNoTracking()
            .Where(pm => pm.ProjectId == request.ProjectId)
            .OrderBy(pm => pm.Role)
            .ThenBy(pm => pm.User.LastName)
            .Select(pm => new ProjectMemberResponse(
                pm.UserId,
                pm.User.Email ?? string.Empty,
                pm.User.FirstName,
                pm.User.LastName,
                pm.Role))
            .ToListAsync(cancellationToken);
    }
}
