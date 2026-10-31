using EventRegistration.Api.Interfaces;
using EventRegistration.Api.Exceptions;
using MediatR;
using Dapper;
namespace EventRegistration.Api.Features.Registrations;

public class CancelRegistrationCommand : IRequest<RegistrationDetailsDto>
{
    public long Id { get; set; }
}

public class CancelRegistrationHandler
    : IRequestHandler<CancelRegistrationCommand, RegistrationDetailsDto>
{
    private readonly IEventRegistrationDatabase _database;

    public CancelRegistrationHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<RegistrationDetailsDto> Handle(
        CancelRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        using var connection = _database.Open();

        const string selectSql = @"
        SELECT
            r.Id AS Id,
            r.EventId AS EventId,
            e.Name AS EventName,
            r.ParticipantId AS ParticipantId,
            p.FullName AS ParticipantName,
            p.Email AS ParticipantEmail,
            p.Phone AS ParticipantPhone,
            r.Status AS Status,
            CASE WHEN r.Status = 1 THEN 'Active' ELSE 'Cancelled' END AS StatusName,
            r.Notes AS Notes,
            r.RegisteredAt AS RegisteredAt,
            r.CancelledAt AS CancelledAt
        FROM Registrations r
        INNER JOIN Participants p ON p.Id = r.ParticipantId
        INNER JOIN Events e ON e.Id = r.EventId
        WHERE r.Id = @Id;";

        var registration = await connection.QuerySingleOrDefaultAsync<(
            long Id,
            int Status,
            DateTime StartAt
        )>(
            @"
            SELECT r.Id, r.Status, e.StartAt
            FROM Registrations r
            INNER JOIN Events e ON e.Id = r.EventId
            WHERE r.Id = @Id;
            ",
            new { request.Id }
        );

        if (registration == default)
        {
            throw new NotFoundException(
                "The registration does not exist."
            );
        }

        if (registration.Status == 2)
          {
            return await connection.QuerySingleAsync<RegistrationDetailsDto>(
            selectSql, new { registration.Id });
          }

        if (DateTime.UtcNow >= registration.StartAt)
        {
            throw new BusinessException(
                "The event has already started. Registration cannot be cancelled."
            );
        }

        await connection.ExecuteAsync(
            @"
            UPDATE Registrations
            SET Status = 2,
                CancelledAt = UTC_TIMESTAMP()
            WHERE Id = @Id
              AND Status = 1;
            ",
            new { request.Id }
        );

return await connection.QuerySingleAsync<RegistrationDetailsDto>(selectSql, new { registration.Id });    }
}