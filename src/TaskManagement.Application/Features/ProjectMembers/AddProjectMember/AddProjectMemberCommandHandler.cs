using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Identity;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Features.ProjectMembers.AddProjectMember;

public class AddProjectMemberCommandHandler : IRequestHandler<AddProjectMemberCommand, ProjectMemberResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IProjectAccessService _projectAccess;
    private readonly IIdentityService _identityService;

    public AddProjectMemberCommandHandler(
        IApplicationDbContext context,
        IProjectAccessService projectAccess,
        IIdentityService identityService)
    {
        _context = context;
        _projectAccess = projectAccess;
        _identityService = identityService;
    }

    public async Task<ProjectMemberResponse> Handle(AddProjectMemberCommand request, CancellationToken cancellationToken)
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

        var user = await _identityService.GetUserByIdAsync(request.UserId);
        if (user is null)
        {
            throw new NotFoundException("User", request.UserId);
        }

        var alreadyMember = await _context.ProjectMembers
            .AnyAsync(pm => pm.ProjectId == request.ProjectId && pm.UserId == request.UserId, cancellationToken);

        if (alreadyMember)
        {
            throw new ConflictException("The user is already a member of this project.");
        }

        var member = new ProjectMember
        {
            ProjectId = request.ProjectId,
            UserId = request.UserId,
            Role = request.Role
        };

        _context.ProjectMembers.Add(member);
        await _context.SaveChangesAsync(cancellationToken);

        return new ProjectMemberResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            member.Role);
    }
}
