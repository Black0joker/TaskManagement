using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Authentication;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Domain.Authorization;

namespace TaskManagement.Application.Features.Projects.ListProjects;

public class ListProjectsQueryHandler : IRequestHandler<ListProjectsQuery, IReadOnlyList<ProjectResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ListProjectsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<ProjectResponse>> Handle(ListProjectsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Array.Empty<ProjectResponse>();
        }

        var query = _context.Projects.AsNoTracking();

        // System administrators see every project; everyone else only sees
        // projects they are members of.
        if (!_currentUserService.IsInRole(ApplicationRoles.Admin))
        {
            query = query.Where(p => p.ProjectMembers.Any(pm => pm.UserId == userId));
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProjectResponse(
                p.Id,
                p.Name,
                p.Description,
                p.CreatedById,
                p.CreatedAt,
                p.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}
