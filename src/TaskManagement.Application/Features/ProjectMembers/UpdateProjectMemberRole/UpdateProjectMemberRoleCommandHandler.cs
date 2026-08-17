using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Features.ProjectMembers.UpdateProjectMemberRole;

public class UpdateProjectMemberRoleCommandHandler : IRequestHandler<UpdateProjectMemberRoleCommand, ProjectMemberResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IProjectAccessService _projectAccess;

    public UpdateProjectMemberRoleCommandHandler(
        IApplicationDbContext context,
        IProjectAccessService projectAccess)
    {
        _context = context;
        _projectAccess = projectAccess;
    }

    public async Task<ProjectMemberResponse> Handle(UpdateProjectMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var projectExists = await _context.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.ProjectId, cancellationToken);

        if (!projectExists)
        {
            throw new NotFoundException("Project", request.ProjectId);
        }

        if (!await _projectAccess.CanManageAsync(request.ProjectId, cancellationToken))
        {
            throw new ForbiddenAccessException("Only project owners and admins can manage members.");
        }

        var member = await _context.ProjectMembers
            .Include(pm => pm.User)
            .FirstOrDefaultAsync(
                pm => pm.ProjectId == request.ProjectId && pm.UserId == request.UserId,
                cancellationToken);

        if (member is null)
        {
            throw new NotFoundException("ProjectMember", $"{request.ProjectId}/{request.UserId}");
        }

        // A project must always keep at least one Owner.
        if (member.Role == ProjectMemberRole.Owner && request.Role != ProjectMemberRole.Owner)
        {
            var ownerCount = await _context.ProjectMembers
                .CountAsync(
                    pm => pm.ProjectId == request.ProjectId && pm.Role == ProjectMemberRole.Owner,
                    cancellationToken);

            if (ownerCount <= 1)
            {
                throw new ForbiddenAccessException("Cannot change the role of the last project owner.");
            }
        }

        member.Role = request.Role;
        await _context.SaveChangesAsync(cancellationToken);

        return new ProjectMemberResponse(
            member.User.Id,
            member.User.Email!,
            member.User.FirstName,
            member.User.LastName,
            member.Role);
    }
}
