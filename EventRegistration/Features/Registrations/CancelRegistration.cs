using EventRegistration.Api.Interfaces;
using EventRegistration.Api.Exceptions;
using MediatR;
using Dapper;
using EventRegistration.Api.Common.Registrations;
namespace EventRegistration.Api.Features.Registrations;

public class CancelRegistrationCommand : IRequest<RegistrationDto>
{
    public long Id { get; set; }
}

public class CancelRegistrationHandler
    : IRequestHandler<CancelRegistrationCommand, RegistrationDto>
{
    private readonly IEventRegistrationDatabase _database;

    public CancelRegistrationHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<RegistrationDto> Handle(
        CancelRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        using var connection = _database.Open();

        var  selectSql = $@"
            {RegistrationQueries.Select}
            WHERE r.Id = @Id;
        ";

        var registration = await connection.QuerySingleOrDefaultAsync<(
            long Id,
            int Status,
            DateTime StartAt
        )>(
            new CommandDefinition(
            @"
            SELECT r.Id, r.Status, e.StartAt
            FROM Registrations r
            INNER JOIN Events e ON e.Id = r.EventId
            WHERE r.Id = @Id;
            ",
            new { request.Id },
            cancellationToken: cancellationToken
            )
        );

        if (registration == default)
        {
            throw new NotFoundException(
                "The registration does not exist."
            );
        }

        if (registration.Status == 2)
          {
            return await connection.QuerySingleAsync<RegistrationDto>(
                new CommandDefinition(
            selectSql, 
            new { registration.Id },
            cancellationToken: cancellationToken
            )

            );
          }

        if (DateTime.UtcNow >= registration.StartAt)
        {
            throw new BusinessException(
                "The event has already started. Registration cannot be cancelled."
            );
        }

        await connection.ExecuteAsync(
             new CommandDefinition(
            @"
            UPDATE Registrations
            SET Status = 2,
                CancelledAt = UTC_TIMESTAMP()
            WHERE Id = @Id
              AND Status = 1;
            ",
            new { request.Id },
            cancellationToken: cancellationToken
             )
        );

return await connection.QuerySingleAsync<RegistrationDto>(
    new CommandDefinition(
     selectSql,
     new { registration.Id },
     cancellationToken: cancellationToken)
     );  
     
     
}
}