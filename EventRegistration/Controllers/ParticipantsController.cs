using EventRegistration.Api.Features.Participants;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EventRegistration.Api.Controllers;

[ApiController]
[Route("api/participants")]
public class ParticipantsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ParticipantsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<GetParticipantsResult>> GetAll(
        [FromQuery] GetParticipantsQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
    [HttpPost]
    public async Task<ActionResult<ParticipantResponse>> Create(
    [FromBody] CreateParticipantCommand command,
    CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Created($"/api/participants/{result.Id}", result);
    }
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ParticipantResponse>> Update(
    long id,
    [FromBody] UpdateParticipantRequest request,
    CancellationToken cancellationToken = default)
    {
        var command = new UpdateParticipantCommand(
            id,
            request.FullName,
            request.Email,
            request.Phone,
            request.DateOfBirth,
            request.IsActive);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new DeleteParticipantCommand(id), cancellationToken);
        return NoContent();
    }
}