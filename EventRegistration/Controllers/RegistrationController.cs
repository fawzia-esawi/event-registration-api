using MediatR;
using Microsoft.AspNetCore.Mvc;


namespace EventRegistration.Api.Controllers
{
    [ApiController]
    [Route("api/events/{eventId:long}/registrations")]
    public class RegistrationController : ControllerBase
    {
        private readonly ISender _sender;

        public RegistrationController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<ActionResult<GetEventRegistrationsResult>>
            GetEventRegistrations(
                [FromRoute] long eventId,
                [FromQuery] GetEventRegistrationsQuery query)
        {
            query.EventId = eventId;

            var result = await _sender.Send(query);

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRegistrations(
            [FromRoute] long eventId,
            [FromBody] CreateRegistrationsCommand command){
                command.EventId = eventId;

               
               var result = await _sender.Send(command);

             return StatusCode(
                  StatusCodes.Status201Created,
                  new{ id = result.Id,message = result.Message});
            }

        
        [HttpPatch("/api/registrations/{id:long}/cancel")]

        public async Task<IActionResult> CancelRegistration(
            [FromRoute] long id)
       {

               var command = new CancelRegistrationCommand{Id = id};
               var result = await _sender.Send(command);
               return Ok(new
               {
                id = result.Id,
                message = result.Message
                });
        }

        [HttpGet("/api/registrations/{id:long}")]

        public async Task<ActionResult<RegistrationDetailsDto>>
        
        GetRegistrationById([FromRoute] long id)
        {
            var query = new GetRegistrationByIdQuery {Id = id};
            var result = await _sender.Send(query);


            return Ok(result);
        }
    }
}