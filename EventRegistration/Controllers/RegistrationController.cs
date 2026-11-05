using MediatR;
using Microsoft.AspNetCore.Mvc;
using EventRegistration.Api.Features.Registrations;
using EventRegistration.Api.Common.Registrations;

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
                [FromQuery] GetEventRegistrationsQuery query,
                CancellationToken cancellationToken
            )
        {
            query.EventId = eventId;

            var result = await _sender.Send(query , cancellationToken);

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRegistrations(
            [FromRoute] long eventId,
            [FromBody] CreateRegistrationsCommand command ,
            CancellationToken cancellationToken
            ){
                command.EventId = eventId;

               
               var result = await _sender.Send(command , cancellationToken);
               

             return CreatedAtAction(
                nameof(GetRegistrationById),
                new { id = result.Id },
                result
            );
            }

        
        [HttpPatch("/api/registrations/{id:long}/cancel")]

        public async Task<IActionResult> CancelRegistration(
            [FromRoute] long id ,
            CancellationToken cancellationToken
            )
       {

               var command = new CancelRegistrationCommand{Id = id};
               var result = await _sender.Send(command , cancellationToken );
               return Ok(result
               );
        }

        [HttpGet("/api/registrations/{id:long}")]

        public async Task<ActionResult<RegistrationDto>>
        
        GetRegistrationById([FromRoute] long id , CancellationToken cancellationToken)
        {
            var query = new GetRegistrationByIdQuery {Id = id};
            var result = await _sender.Send(query , cancellationToken );


            return Ok(result);
        }
    }
}