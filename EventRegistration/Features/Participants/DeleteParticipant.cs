using Dapper;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using MediatR;
using MySqlConnector;

namespace EventRegistration.Api.Features.Participants;

public record DeleteParticipantCommand(long Id) : IRequest;

public class DeleteParticipantHandler : IRequestHandler<DeleteParticipantCommand>
{
    private const string HasHistoryMessage =
        "This participant has registration history and cannot be deleted.";

    private readonly IEventRegistrationDatabase _database;

    public DeleteParticipantHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task Handle(DeleteParticipantCommand request, CancellationToken cancellationToken)
    {
        using var connection = _database.Open();

        const string existsSql = "SELECT COUNT(1) FROM Participants WHERE Id = @Id;";

        var exists = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(existsSql, new { request.Id }, cancellationToken: cancellationToken));

        if (exists == 0)
            throw new NotFoundException($"Participant with id {request.Id} was not found.");

        const string usedSql = "SELECT COUNT(1) FROM Registrations WHERE ParticipantId = @Id;";

        var registrations = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(usedSql, new { request.Id }, cancellationToken: cancellationToken));

        if (registrations > 0)
            throw new BusinessException(HasHistoryMessage);

        const string deleteSql = "DELETE FROM Participants WHERE Id = @Id;";

        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(deleteSql, new { request.Id }, cancellationToken: cancellationToken));
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.RowIsReferenced2)
        {
            throw new BusinessException(HasHistoryMessage);
        }
    }
}