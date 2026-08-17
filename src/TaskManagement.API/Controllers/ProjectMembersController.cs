using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Features.ProjectMembers;
using TaskManagement.Application.Features.ProjectMembers.AddProjectMember;
using TaskManagement.Application.Features.ProjectMembers.GetProjectMembers;
using TaskManagement.Application.Features.ProjectMembers.RemoveProjectMember;
using TaskManagement.Application.Features.ProjectMembers.UpdateProjectMemberRole;
using TaskManagement.Domain.Authorization;
using TaskManagement.Domain.Enums;

namespace TaskManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/members")]
[Authorize]
public class ProjectMembersController : ControllerBase
{
    private readonly ISender _sender;

    public ProjectMembersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = ApplicationPermissions.Projects.Read)]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectMemberResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ProjectMemberResponse>>> GetMembers(
        string projectId,
        CancellationToken cancellationToken)
    {
        var members = await _sender.Send(new GetProjectMembersQuery(projectId), cancellationToken);
        return Ok(members);
    }

    [HttpPost]
    [Authorize(Policy = ApplicationPermissions.Projects.Update)]
    [ProducesResponseType(typeof(ProjectMemberResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectMemberResponse>> AddMember(
        string projectId,
        [FromBody] AddProjectMemberRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddProjectMemberCommand(projectId, request.UserId, request.Role);
        var member = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(
            nameof(GetMembers),
            new { projectId },
            member);
    }

    [HttpPut("{userId}")]
    [Authorize(Policy = ApplicationPermissions.Projects.Update)]
    [ProducesResponseType(typeof(ProjectMemberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectMemberResponse>> UpdateMemberRole(
        string projectId,
        string userId,
        [FromBody] UpdateProjectMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProjectMemberRoleCommand(projectId, userId, request.Role);
        var member = await _sender.Send(command, cancellationToken);
        return Ok(member);
    }

    [HttpDelete("{userId}")]
    [Authorize(Policy = ApplicationPermissions.Projects.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(
        string projectId,
        string userId,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new RemoveProjectMemberCommand(projectId, userId), cancellationToken);
        return NoContent();
    }
}

public sealed record AddProjectMemberRequest(string UserId, ProjectMemberRole Role);

public sealed record UpdateProjectMemberRoleRequest(ProjectMemberRole Role);
