using EventRegistration.Api.Interfaces;
using EventRegistration.Api.Exceptions;
using MediatR;
using Dapper;

public class CancelRegistrationCommand : IRequest<RegistrationResult>
{
    public long Id { get; set; }
}

public class CancelRegistrationHandler
    : IRequestHandler<CancelRegistrationCommand, RegistrationResult>
{
    private readonly IEventRegistrationDatabase _database;

    public CancelRegistrationHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<RegistrationResult> Handle(
        CancelRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        using var connection = _database.Open();

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
            return new RegistrationResult
            {
                Id = registration.Id,
                Message = "Registration is already cancelled."
            };
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

        return new RegistrationResult
        {
            Id = registration.Id,
            Message = "Registration cancelled successfully."
        };
    }
}