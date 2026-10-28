using MediatR;
using Dapper;
using EventRegistration.Api.Interfaces;
using EventRegistration.Api.Exceptions;


namespace EventRegistration.Api.Features.Events;

public class DeleteEventCommand : IRequest<long>
{
    public long Id { get; set; }
}
public class DeleteEventHandler
    : IRequestHandler<DeleteEventCommand, long>
{
    private readonly IEventRegistrationDatabase _database;

    public DeleteEventHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<long> Handle(
        DeleteEventCommand request,
        CancellationToken cancellationToken)
    {
        using var connection = _database.Open();

        const string checkEventSql = @"
            SELECT EXISTS(
                SELECT 1
                FROM Events
                WHERE Id = @Id
            );
        ";

        var eventExists = await connection.ExecuteScalarAsync<bool>(
            checkEventSql,
            new { request.Id }
        );

        if (!eventExists)
           {
                 throw new NotFoundException(
                 $"Event with ID {request.Id} was not found."
                );
           }
        
        const string checkRegistrationsSql = @"
            SELECT COUNT(*)
            FROM Registrations
            WHERE EventId = @Id;
             ";

        var registrationCount =await connection.ExecuteScalarAsync<int>(checkRegistrationsSql,new { request.Id });

        if (registrationCount > 0)
            {
                   throw new BusinessException(
                  "This event cannot be deleted because it has registrations."
                    );
            }

  
        const string deleteSql = @"
            DELETE FROM Events
            WHERE Id = @Id;
        ";

        await connection.ExecuteAsync(
            deleteSql,
            new { request.Id }
        );

        return request.Id;
    }
}