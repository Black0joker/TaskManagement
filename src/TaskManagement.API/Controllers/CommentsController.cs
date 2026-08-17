using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Features.Comments;
using TaskManagement.Application.Features.Comments.DeleteComment;
using TaskManagement.Application.Features.Comments.UpdateComment;
using TaskManagement.Domain.Authorization;

namespace TaskManagement.API.Controllers;

[ApiController]
[Route("api/comments")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly ISender _sender;

    public CommentsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPut("{id}")]
    [Authorize(Policy = ApplicationPermissions.Comments.Update)]
    [ProducesResponseType(typeof(CommentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentResponse>> UpdateComment(
        string id,
        [FromBody] UpdateCommentRequest request,
        CancellationToken cancellationToken)
    {
        var comment = await _sender.Send(
            new UpdateCommentCommand(id, request.Content),
            cancellationToken);
        return Ok(comment);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = ApplicationPermissions.Comments.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment(string id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteCommentCommand(id), cancellationToken);
        return NoContent();
    }
}

public sealed record UpdateCommentRequest(string Content);
