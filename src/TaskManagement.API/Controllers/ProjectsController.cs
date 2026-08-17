using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Features.Projects;
using TaskManagement.Application.Features.Projects.CreateProject;
using TaskManagement.Application.Features.Projects.DeleteProject;
using TaskManagement.Application.Features.Projects.GetProject;
using TaskManagement.Application.Features.Labels.CreateProjectLabel;
using TaskManagement.Application.Features.Projects.GetProjectLabels;
using TaskManagement.Application.Features.Projects.GetProjectTasks;
using TaskManagement.Application.Features.Projects.ListProjects;
using TaskManagement.Application.Features.Projects.UpdateProject;
using TaskManagement.Domain.Authorization;

namespace TaskManagement.API.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly ISender _sender;

    public ProjectsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = ApplicationPermissions.Projects.Read)]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProjectResponse>>> ListProjects(CancellationToken cancellationToken)
    {
        var projects = await _sender.Send(new ListProjectsQuery(), cancellationToken);
        return Ok(projects);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = ApplicationPermissions.Projects.Read)]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectResponse>> GetProject(string id, CancellationToken cancellationToken)
    {
        var project = await _sender.Send(new GetProjectQuery(id), cancellationToken);
        return Ok(project);
    }

    [HttpPost]
    [Authorize(Policy = ApplicationPermissions.Projects.Create)]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProjectResponse>> CreateProject(
        [FromBody] CreateProjectCommand command,
        CancellationToken cancellationToken)
    {
        var project = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = ApplicationPermissions.Projects.Update)]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectResponse>> UpdateProject(
        string id,
        [FromBody] UpdateProjectCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(
                    nameof(command.Id),
                    "The route id does not match the project id in the request body.")
            });
        }

        var project = await _sender.Send(command, cancellationToken);
        return Ok(project);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = ApplicationPermissions.Projects.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProject(string id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteProjectCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpGet("{id}/tasks")]
    [Authorize(Policy = ApplicationPermissions.Projects.Read)]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectTaskSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ProjectTaskSummary>>> GetProjectTasks(string id, CancellationToken cancellationToken)
    {
        var tasks = await _sender.Send(new GetProjectTasksQuery(id), cancellationToken);
        return Ok(tasks);
    }

    [HttpGet("{id}/labels")]
    [Authorize(Policy = ApplicationPermissions.Projects.Read)]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectLabelSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ProjectLabelSummary>>> GetProjectLabels(string id, CancellationToken cancellationToken)
    {
        var labels = await _sender.Send(new GetProjectLabelsQuery(id), cancellationToken);
        return Ok(labels);
    }

    [HttpPost("{id}/labels")]
    [Authorize(Policy = ApplicationPermissions.Projects.Update)]
    [ProducesResponseType(typeof(ProjectLabelSummary), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectLabelSummary>> CreateProjectLabel(
        string id,
        [FromBody] CreateProjectLabelRequest request,
        CancellationToken cancellationToken)
    {
        var label = await _sender.Send(
            new CreateProjectLabelCommand(id, request.Name, request.Color),
            cancellationToken);
        return Created($"/api/projects/{id}/labels", label);
    }
}

public sealed record CreateProjectLabelRequest(string Name, string Color);
