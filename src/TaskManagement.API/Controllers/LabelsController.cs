using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Features.Labels.DeleteLabel;
using TaskManagement.Application.Features.Labels.UpdateLabel;
using TaskManagement.Application.Features.Projects;
using TaskManagement.Domain.Authorization;

namespace TaskManagement.API.Controllers;

[ApiController]
[Route("api/labels")]
[Authorize]
public class LabelsController : ControllerBase
{
    private readonly ISender _sender;

    public LabelsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPut("{id}")]
    [Authorize(Policy = ApplicationPermissions.Projects.Update)]
    [ProducesResponseType(typeof(ProjectLabelSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectLabelSummary>> UpdateLabel(
        string id,
        [FromBody] UpdateLabelRequest request,
        CancellationToken cancellationToken)
    {
        var label = await _sender.Send(
            new UpdateLabelCommand(id, request.Name, request.Color),
            cancellationToken);
        return Ok(label);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = ApplicationPermissions.Projects.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLabel(string id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteLabelCommand(id), cancellationToken);
        return NoContent();
    }
}

public sealed record UpdateLabelRequest(string Name, string Color);
