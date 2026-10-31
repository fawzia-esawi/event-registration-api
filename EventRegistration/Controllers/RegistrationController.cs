using MediatR;
using Microsoft.AspNetCore.Mvc;
using EventRegistration.Api.Features.Registrations;

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
               Response.Headers.Location = $"/api/registrations/{result.Id}";

             return StatusCode(
                  StatusCodes.Status201Created,
                  result);
            }

        
        [HttpPatch("/api/registrations/{id:long}/cancel")]

        public async Task<IActionResult> CancelRegistration(
            [FromRoute] long id)
       {

               var command = new CancelRegistrationCommand{Id = id};
               var result = await _sender.Send(command);
               return Ok(result
               );
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