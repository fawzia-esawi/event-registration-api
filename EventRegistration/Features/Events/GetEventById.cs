using Dapper;
using MediatR;
using EventRegistration.Api.Interfaces;
using EventRegistration.Api.Exceptions;
using FluentValidation;
namespace EventRegistration.Api.Features.Events;

public class GetEventByIdQuery : IRequest<EventDetailsDto>
{
    public long Id { get; set; }
}
public class GetEventByIdQueryValidator
    : AbstractValidator<GetEventByIdQuery>
{
    public GetEventByIdQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

public class EventDetailsDto
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public long CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public DateTime RegistrationDeadline { get; set; }

    public int Capacity { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public class GetEventByIdHandler
    : IRequestHandler<GetEventByIdQuery, EventDetailsDto>
{
    private readonly IEventRegistrationDatabase _database;

    public GetEventByIdHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<EventDetailsDto> Handle(
        GetEventByIdQuery request,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                e.Id,
                e.Name,
                e.CategoryId,
                c.Name AS CategoryName,
                e.Description,
                e.Location,
                e.StartAt,
                e.EndAt,
                e.RegistrationDeadline,
                e.Capacity,
                e.IsActive,
                e.CreatedAt,
                e.UpdatedAt
            FROM Events e
            INNER JOIN Categories c ON e.CategoryId = c.Id
            WHERE e.Id = @Id;
        ";

        using var connection = _database.Open();

        var eventDetails = await connection.QuerySingleOrDefaultAsync<EventDetailsDto>(
            sql,
            new { request.Id }
        );

        if (eventDetails is null)
        {
            throw new NotFoundException(
            $"Event with ID {request.Id} was not found."
            );
        }

        return eventDetails;
    }
}
 