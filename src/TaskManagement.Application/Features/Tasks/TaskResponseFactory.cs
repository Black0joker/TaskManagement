using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Features.Tasks;

/// <summary>
/// Maps a tracked <see cref="TaskItem"/> to <see cref="TaskResponse"/>,
/// resolving the assignee's display name from the database.
/// </summary>
internal static class TaskResponseFactory
{
    public static async Task<TaskResponse> CreateAsync(
        TaskItem task,
        IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        TaskAssigneeDto? assignee = null;

        if (task.AssignedToId is not null)
        {
            assignee = await context.Users
                .AsNoTracking()
                .Where(u => u.Id == task.AssignedToId)
                .Select(u => new TaskAssigneeDto(u.Id, u.FirstName + " " + u.LastName))
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new TaskResponse(
            task.Id,
            task.ProjectId,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.DueDate,
            assignee,
            task.CreatedById,
            task.CreatedAt,
            task.UpdatedAt);
    }
}
