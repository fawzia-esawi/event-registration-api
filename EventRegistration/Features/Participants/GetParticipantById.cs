using Dapper;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using MediatR;

namespace EventRegistration.Api.Features.Participants;

public record GetParticipantByIdQuery(long Id) : IRequest<ParticipantResponse>;

public class GetParticipantByIdHandler : IRequestHandler<GetParticipantByIdQuery, ParticipantResponse>
{
    private readonly IEventRegistrationDatabase _database;

    public GetParticipantByIdHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<ParticipantResponse> Handle(
        GetParticipantByIdQuery request,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT Id, FullName, Email, Phone,
                   DATE_FORMAT(DateOfBirth, '%Y-%m-%d') AS DateOfBirth,
                   IsActive, CreatedAt, UpdatedAt
            FROM Participants
            WHERE Id = @Id;";

        using var connection = _database.Open();

        var participant = await connection.QuerySingleOrDefaultAsync<ParticipantResponse>(
            new CommandDefinition(sql, new { request.Id }, cancellationToken: cancellationToken));

        return participant
            ?? throw new NotFoundException($"Participant with id {request.Id} was not found.");
    }
}