using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Common.Pagination;
using TaskManagement.Application.Features.Comments;
using TaskManagement.Application.Features.Comments.CreateComment;
using TaskManagement.Application.Features.Comments.GetTaskComments;
using TaskManagement.Application.Features.Tasks;
using TaskManagement.Application.Features.Tasks.AssignLabelToTask;
using TaskManagement.Application.Features.Tasks.CreateTask;
using TaskManagement.Application.Features.Tasks.DeleteTask;
using TaskManagement.Application.Features.Tasks.GetTask;
using TaskManagement.Application.Features.Tasks.ListTasks;
using TaskManagement.Application.Features.Tasks.RemoveLabelFromTask;
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
    [ProducesResponseType(typeof(PagedResult<TaskResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TaskResponse>>> GetTasks(
        [FromQuery] string? projectId,
        [FromQuery] bool overdue = false,
        [FromQuery] bool dueToday = false,
        [FromQuery] bool dueThisWeek = false,
        [FromQuery] bool noDueDate = false,
        [FromQuery] DateTime? dueBefore = null,
        [FromQuery] DateTime? dueAfter = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PaginationParameters.DefaultPageSize,
        [FromQuery] TaskItemStatus? status = null,
        [FromQuery] TaskItemPriority? priority = null,
        [FromQuery] string? assignedToId = null,
        [FromQuery] string? createdById = null,
        [FromQuery] string? labelId = null,
        [FromQuery] DateTime? dueFrom = null,
        [FromQuery] DateTime? dueTo = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var tasks = await _sender.Send(
            new ListTasksQuery(
                projectId,
                overdue,
                dueToday,
                dueThisWeek,
                noDueDate,
                dueBefore,
                dueAfter,
                page,
                pageSize,
                status,
                priority,
                assignedToId,
                createdById,
                labelId,
                dueFrom,
                dueTo,
                sortBy,
                sortDirection,
                search),
            cancellationToken);
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

    [HttpPost("{id}/labels/{labelId}")]
    [Authorize(Policy = ApplicationPermissions.Tasks.Update)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignLabel(string id, string labelId, CancellationToken cancellationToken)
    {
        await _sender.Send(new AssignLabelToTaskCommand(id, labelId), cancellationToken);
        return Created($"/api/tasks/{id}", (object?)null);
    }

    [HttpDelete("{id}/labels/{labelId}")]
    [Authorize(Policy = ApplicationPermissions.Tasks.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveLabel(string id, string labelId, CancellationToken cancellationToken)
    {
        await _sender.Send(new RemoveLabelFromTaskCommand(id, labelId), cancellationToken);
        return NoContent();
    }

    [HttpGet("{id}/comments")]
    [Authorize(Policy = ApplicationPermissions.Tasks.Read)]
    [ProducesResponseType(typeof(PagedResult<CommentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<CommentResponse>>> ListTaskComments(
        string id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PaginationParameters.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var comments = await _sender.Send(
            new GetTaskCommentsQuery(id, new PaginationParameters(page, pageSize)),
            cancellationToken);
        return Ok(comments);
    }

    [HttpPost("{id}/comments")]
    [Authorize(Policy = ApplicationPermissions.Comments.Create)]
    [ProducesResponseType(typeof(CommentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentResponse>> CreateComment(
        string id,
        [FromBody] CreateCommentRequest request,
        CancellationToken cancellationToken)
    {
        var comment = await _sender.Send(
            new CreateCommentCommand(id, request.Content),
            cancellationToken);
        return Created(string.Empty, comment);
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

public sealed record CreateCommentRequest(string Content);

