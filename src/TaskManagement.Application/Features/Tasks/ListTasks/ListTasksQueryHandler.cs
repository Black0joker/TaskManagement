using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Authentication;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Pagination;
using TaskManagement.Domain.Authorization;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Features.Tasks.ListTasks;

public class ListTasksQueryHandler : IRequestHandler<ListTasksQuery, PagedResult<TaskResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IProjectAccessService _projectAccess;
    private readonly ICurrentUserService _currentUserService;

    public ListTasksQueryHandler(
        IApplicationDbContext context,
        IProjectAccessService projectAccess,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _projectAccess = projectAccess;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<TaskResponse>> Handle(ListTasksQuery request, CancellationToken cancellationToken)
    {
        var pagination = new PaginationParameters(request.Page, request.PageSize);

        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return new PagedResult<TaskResponse>(Array.Empty<TaskResponse>(), pagination.NormalizedPage, pagination.NormalizedPageSize, 0);
        }

        var isSystemAdmin = _currentUserService.IsInRole(ApplicationRoles.Admin);

        var query = _context.TaskItems.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.ProjectId))
        {
            var projectExists = await _context.Projects
                .AsNoTracking()
                .AnyAsync(p => p.Id == request.ProjectId, cancellationToken);

            if (!projectExists)
            {
                throw new NotFoundException("Project", request.ProjectId);
            }

            if (!await _projectAccess.CanReadAsync(request.ProjectId, cancellationToken))
            {
                throw new ForbiddenAccessException("You do not have access to this project.");
            }

            query = query.Where(t => t.ProjectId == request.ProjectId);
        }
        else if (!isSystemAdmin)
        {
            // Non-admins only see tasks from projects they are members of.
            query = query.Where(t =>
                t.Project.ProjectMembers.Any(pm => pm.UserId == userId));
        }

        query = ApplyDueDateFilters(query, request);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((pagination.NormalizedPage - 1) * pagination.NormalizedPageSize)
            .Take(pagination.NormalizedPageSize)
            .Select(t => new TaskResponse(
                t.Id,
                t.ProjectId,
                t.Title,
                t.Description,
                t.Status,
                t.Priority,
                t.DueDate,
                t.AssignedToId,
                t.CreatedById,
                t.CreatedAt,
                t.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<TaskResponse>(items, pagination.NormalizedPage, pagination.NormalizedPageSize, totalCount);
    }

    private static IQueryable<Domain.Entities.TaskItem> ApplyDueDateFilters(
        IQueryable<Domain.Entities.TaskItem> query,
        ListTasksQuery request)
    {
        var today = DateTime.UtcNow.Date;

        // Overdue: past the due date and still open (completed or cancelled
        // work is never reported as overdue).
        if (request.Overdue)
        {
            query = query.Where(t =>
                t.DueDate != null &&
                t.DueDate.Value.Date < today &&
                t.Status != TaskItemStatus.Done &&
                t.Status != TaskItemStatus.Cancelled);
        }

        if (request.DueToday)
        {
            query = query.Where(t => t.DueDate != null && t.DueDate.Value.Date == today);
        }

        // Rolling 7-day window starting today (culture-independent).
        if (request.DueThisWeek)
        {
            var weekEnd = today.AddDays(7);
            query = query.Where(t =>
                t.DueDate != null &&
                t.DueDate.Value.Date >= today &&
                t.DueDate.Value.Date < weekEnd);
        }

        if (request.NoDueDate)
        {
            query = query.Where(t => t.DueDate == null);
        }

        if (request.DueBefore.HasValue)
        {
            var dueBefore = request.DueBefore.Value;
            query = query.Where(t => t.DueDate != null && t.DueDate.Value < dueBefore);
        }

        if (request.DueAfter.HasValue)
        {
            var dueAfter = request.DueAfter.Value;
            query = query.Where(t => t.DueDate != null && t.DueDate.Value > dueAfter);
        }

        return query;
    }
}
