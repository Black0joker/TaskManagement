using MediatR;
using Microsoft.Extensions.Logging;
using TaskManagement.Application.Abstractions.Authentication;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Features.Projects.CreateProject;

public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, ProjectResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateProjectCommandHandler> _logger;

    public CreateProjectCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<CreateProjectCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ProjectResponse> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            CreatedById = _currentUserService.UserId
        };

        _context.Projects.Add(project);

        // The creator becomes the project Owner.
        _context.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = project.Id,
            UserId = _currentUserService.UserId,
            Role = ProjectMemberRole.Owner
        });

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Project created ({ProjectId}, {ProjectName}) by user {UserId}",
            project.Id,
            project.Name,
            _currentUserService.UserId);

        return new ProjectResponse(
            project.Id,
            project.Name,
            project.Description,
            project.CreatedById,
            project.CreatedAt,
            project.UpdatedAt);
    }
}
