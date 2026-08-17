using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskManagement.Application.Abstractions.Authentication;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Features.Tasks.CreateTask;

public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IProjectAccessService _projectAccess;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateTaskCommandHandler> _logger;

    public CreateTaskCommandHandler(
        IApplicationDbContext context,
        IProjectAccessService projectAccess,
        ICurrentUserService currentUserService,
        ILogger<CreateTaskCommandHandler> logger)
    {
        _context = context;
        _projectAccess = projectAccess;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<TaskResponse> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var projectExists = await _context.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.ProjectId, cancellationToken);

        if (!projectExists)
        {
            throw new NotFoundException("Project", request.ProjectId);
        }

        if (!await _projectAccess.CanContributeAsync(request.ProjectId, cancellationToken))
        {
            throw new ForbiddenAccessException("Only project owners, admins and members can create tasks.");
        }

        var assignedToId = string.IsNullOrWhiteSpace(request.AssignedToId) ? null : request.AssignedToId;

        if (assignedToId is not null)
        {
            var assigneeIsMember = await _context.ProjectMembers
                .AnyAsync(
                    pm => pm.ProjectId == request.ProjectId && pm.UserId == assignedToId,
                    cancellationToken);

            if (!assigneeIsMember)
            {
                throw new ValidationException(new[]
                {
                    new FluentValidation.Results.ValidationFailure(
                        nameof(request.AssignedToId),
                        "The assigned user must be a member of the project.")
                });
            }
        }

        var task = new TaskItem
        {
            ProjectId = request.ProjectId,
            Title = request.Title,
            Description = request.Description,
            Status = request.Status,
            Priority = request.Priority,
            AssignedToId = assignedToId,
            DueDate = request.DueDate,
            CreatedById = _currentUserService.UserId
        };

        _context.TaskItems.Add(task);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Task created ({TaskId}) in project {ProjectId} by user {UserId}",
            task.Id,
            task.ProjectId,
            _currentUserService.UserId);

        return await TaskResponseFactory.CreateAsync(task, _context, cancellationToken);
    }
}
