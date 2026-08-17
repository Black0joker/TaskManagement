using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Features.Tasks;
using TaskManagement.Application.Features.Tasks.CreateTask;
using TaskManagement.Application.Features.Tasks.DeleteTask;
using TaskManagement.Application.Features.Tasks.GetTask;
using TaskManagement.Application.Features.Tasks.ListTasks;
using TaskManagement.Application.Features.Tasks.UpdateTask;
using TaskManagement.Application.Features.Tasks.UpdateTaskAssignee;
using TaskManagement.Application.Features.Tasks.UpdateTaskPriority;
using TaskManagement.Application.Features.Tasks.UpdateTaskStatus;
using TaskManagement.Domain.Authorization;
using TaskManagement.Domain.Enums;

namespace TaskManagement.API.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ISender _sender;

    public TasksController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = ApplicationPermissions.Tasks.Read)]
    [ProducesResponseType(typeof(IReadOnlyList<TaskResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> GetTasks(
        [FromQuery] string? projectId,
        CancellationToken cancellationToken)
    {
        var tasks = await _sender.Send(new ListTasksQuery(projectId), cancellationToken);
        return Ok(tasks);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = ApplicationPermissions.Tasks.Read)]
    [ProducesResponseType(typeof(TaskDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDetailsResponse>> GetTask(
        string id,
        CancellationToken cancellationToken)
    {
        var task = await _sender.Send(new GetTaskQuery(id), cancellationToken);
        return Ok(task);
    }

    [HttpPost]
    [Authorize(Policy = ApplicationPermissions.Tasks.Create)]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> CreateTask(
        [FromBody] CreateTaskCommand request,
        CancellationToken cancellationToken)
    {
        var task = await _sender.Send(request, cancellationToken);
        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = ApplicationPermissions.Tasks.Update)]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> UpdateTask(
        string id,
        [FromBody] UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTaskCommand(
            id,
            request.Title,
            request.Description,
            request.Status,
            request.Priority,
            request.AssignedToId,
            request.DueDate);

        var task = await _sender.Send(command, cancellationToken);
        return Ok(task);
    }

    [HttpPatch("{id}/status")]
    [Authorize(Policy = ApplicationPermissions.Tasks.Update)]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> UpdateTaskStatus(
        string id,
        [FromBody] UpdateTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTaskStatusCommand(id, request.Status);
        var task = await _sender.Send(command, cancellationToken);
        return Ok(task);
    }

    [HttpPatch("{id}/priority")]
    [Authorize(Policy = ApplicationPermissions.Tasks.Update)]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> UpdateTaskPriority(
        string id,
        [FromBody] UpdateTaskPriorityRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTaskPriorityCommand(id, request.Priority);
        var task = await _sender.Send(command, cancellationToken);
        return Ok(task);
    }

    [HttpPatch("{id}/assignee")]
    [Authorize(Policy = ApplicationPermissions.Tasks.Update)]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> UpdateTaskAssignee(
        string id,
        [FromBody] UpdateTaskAssigneeRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTaskAssigneeCommand(id, request.UserId);
        var task = await _sender.Send(command, cancellationToken);
        return Ok(task);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = ApplicationPermissions.Tasks.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTask(
        string id,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteTaskCommand(id), cancellationToken);
        return NoContent();
    }
}

public sealed record UpdateTaskRequest(
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskItemPriority Priority,
    string? AssignedToId,
    DateTime? DueDate);

public sealed record UpdateTaskStatusRequest(TaskItemStatus? Status);

public sealed record UpdateTaskPriorityRequest(TaskItemPriority? Priority);

public sealed record UpdateTaskAssigneeRequest(string? UserId);
