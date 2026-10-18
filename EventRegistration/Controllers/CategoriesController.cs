using EventRegistration.Api.Features.Categories;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EventRegistration.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoryResponse>>> GetAll(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetCategoriesQuery(includeInactive),
            cancellationToken);

        return Ok(result);
    }
    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(
    [FromBody] CreateCategoryCommand command,
    CancellationToken cancellationToken = default)
    {
        var created = await _mediator.Send(command, cancellationToken);
        return Created($"/api/categories/{created.Id}", created);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CategoryResponse>> GetById(
        long id,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetCategoryByIdQuery(id), cancellationToken);
        return Ok(result);
    }
}