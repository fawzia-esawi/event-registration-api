using Dapper;
using MediatR;
using EventRegistration.Api.Interfaces;
using EventRegistration.Api.Exceptions;
using FluentValidation;
namespace EventRegistration.Api.Features.Registrations;
public class GetRegistrationByIdQuery
    : IRequest<RegistrationDetailsDto>
{
    public long Id { get; set; }
}

public class GetRegistrationByIdQueryValidator
    : AbstractValidator<GetRegistrationByIdQuery>
{
    public GetRegistrationByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Registration ID must be greater than 0.");
    }
}
public class RegistrationDetailsDto
{
    public long Id { get; set; }
    public long EventId { get; set; }
    public string EventName { get; set; } 
    public long ParticipantId { get; set; } 
    public string ParticipantName { get; set; }
    public string ParticipantEmail { get; set; }
    public string ParticipantPhone { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; }
    public string? Notes { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}
public class GetRegistrationByIdHandler
    : IRequestHandler<GetRegistrationByIdQuery, RegistrationDetailsDto>
{
    private readonly IEventRegistrationDatabase _database;

    public GetRegistrationByIdHandler(
        IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<RegistrationDetailsDto> Handle(
        GetRegistrationByIdQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = _database.Open();

        const string sql = @"
        SELECT
          r.Id AS Id,
          r.EventId AS EventId,
          e.Name AS EventName,
          r.ParticipantId AS ParticipantId,
          p.FullName  AS ParticipantName,
          p.Email AS ParticipantEmail,
          p.Phone AS ParticipantPhone,
          r.Status AS Status,
          CASE
             WHEN r.Status = 1 THEN 'Active'
             WHEN r.Status = 2 THEN 'Cancelled'
             ELSE 'Unknown'
          END AS StatusName,
          r.Notes AS Notes,
          r.RegisteredAt AS RegisteredAt,
          r.CancelledAt AS CancelledAt
        FROM Registrations r
        INNER JOIN Participants p ON p.Id = r.ParticipantId
        INNER JOIN Events e ON e.Id = r.EventId
        WHERE r.Id = @Id;
        ";

        var registration =
            await connection.QuerySingleOrDefaultAsync<RegistrationDetailsDto>(
                sql,
                new { request.Id }
            );

        if (registration is null)
        {
            throw new NotFoundException(
                $"Registration with ID {request.Id} was not found."
            );
        }

        return registration;
    }
}