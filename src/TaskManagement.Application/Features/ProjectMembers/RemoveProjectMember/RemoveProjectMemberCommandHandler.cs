using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Authentication;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Features.ProjectMembers.RemoveProjectMember;

public class RemoveProjectMemberCommandHandler : IRequestHandler<RemoveProjectMemberCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IProjectAccessService _projectAccess;
    private readonly ICurrentUserService _currentUserService;

    public RemoveProjectMemberCommandHandler(
        IApplicationDbContext context,
        IProjectAccessService projectAccess,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _projectAccess = projectAccess;
        _currentUserService = currentUserService;
    }

    public async Task Handle(RemoveProjectMemberCommand request, CancellationToken cancellationToken)
    {
        var projectExists = await _context.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.ProjectId, cancellationToken);

        if (!projectExists)
        {
            throw new NotFoundException("Project", request.ProjectId);
        }

        // Project owners/admins can remove anyone; any member may remove themselves (leave).
        var isSelfRemoval = _currentUserService.UserId == request.UserId;
        if (!isSelfRemoval && !await _projectAccess.CanManageAsync(request.ProjectId, cancellationToken))
        {
            throw new ForbiddenAccessException("Only project owners and admins can remove other members.");
        }

        var member = await _context.ProjectMembers
            .FirstOrDefaultAsync(
                pm => pm.ProjectId == request.ProjectId && pm.UserId == request.UserId,
                cancellationToken);

        if (member is null)
        {
            throw new NotFoundException("ProjectMember", $"{request.ProjectId}/{request.UserId}");
        }

        // A project must always keep at least one Owner.
        if (member.Role == ProjectMemberRole.Owner)
        {
            var ownerCount = await _context.ProjectMembers
                .CountAsync(
                    pm => pm.ProjectId == request.ProjectId && pm.Role == ProjectMemberRole.Owner,
                    cancellationToken);

            if (ownerCount <= 1)
            {
                throw new ForbiddenAccessException("Cannot remove the last project owner.");
            }
        }

        _context.ProjectMembers.Remove(member);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
