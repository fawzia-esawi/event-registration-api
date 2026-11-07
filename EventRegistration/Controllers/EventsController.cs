
using MediatR;
using Microsoft.AspNetCore.Mvc;
using EventRegistration.Api.Features.Events;

namespace EventRegistration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly ISender _sender;

    public EventsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<GetEventsResult>> GetEvents(
        [FromQuery] GetEventsQuery query , CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query , cancellationToken);

        return Ok(result);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateEvent(
           [FromBody] CreateEventCommand command , CancellationToken cancellationToken)
     {
       var createdEvent = await _sender.Send(command , cancellationToken);

        return CreatedAtAction(
          nameof(GetEventById),
          new { id = createdEvent.Id },
          createdEvent
        );

     }

    [HttpGet("{id:long}")]
      public async Task<ActionResult<EventDetailsDto>> GetEventById(long id , CancellationToken cancellationToken)
      {
        var result = await _sender.Send(
         new GetEventByIdQuery { Id = id } , cancellationToken );

         return Ok(result);
      }

    
    [HttpPut("{id:long}")]

      public async Task<IActionResult> UpdateEvent(
      long id,
      [FromBody] UpdateEventCommand command , 
      CancellationToken cancellationToken)
     {
       command.Id = id;

       var result = await _sender.Send(command , cancellationToken);

          return Ok(result);
     }

    [HttpDelete("{id:long}")]
       public async Task<IActionResult> DeleteEvent(long id , CancellationToken cancellationToken)
       {
         await _sender.Send(new DeleteEventCommand { Id = id } , cancellationToken);

         return NoContent();
        }
}
