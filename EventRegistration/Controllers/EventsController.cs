
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
        [FromQuery] GetEventsQuery query)
    {
        var result = await _sender.Send(query);

        return Ok(result);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateEvent(
           [FromBody] CreateEventCommand command)
     {
       var eventId = await _sender.Send(command);

        return StatusCode(
        StatusCodes.Status201Created,new { id = eventId , message = "Event created successfully."}
        );

     }

    [HttpGet("{id:long}")]
      public async Task<ActionResult<EventDetailsDto>> GetEventById(long id)
      {
        var result = await _sender.Send(
         new GetEventByIdQuery { Id = id });

         return Ok(result);
      }

    
    [HttpPut("{id:long}")]

      public async Task<IActionResult> UpdateEvent(
      long id,
      [FromBody] UpdateEventCommand command)
     {
       command.Id = id;

       await _sender.Send(command);

          return Ok();
     }

    [HttpDelete("{id:long}")]
       public async Task<IActionResult> DeleteEvent(long id)
       {
         await _sender.Send(new DeleteEventCommand { Id = id });

         return NoContent();
        }
}
