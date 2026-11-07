using EventRegistration.Api.Interfaces;
using EventRegistration.Api.Exceptions;
using MediatR;
using Dapper;
using FluentValidation;
using System.Text;
using MySqlConnector;
using EventRegistration.Api.Common.Registrations;
namespace EventRegistration.Api.Features.Registrations;


public class CreateRegistrationsCommand: IRequest<RegistrationDto>
{
    public long EventId { get; set; } 
    public long ParticipantId { get; set; }  
    public string? Notes {get;set;}
}

 

public class ExistingRegistration
{
    public long Id { get; set; }
    public int Status { get; set; }
}

public class CreateRegistrationsCommandValidator
    : AbstractValidator<CreateRegistrationsCommand>
{
    public CreateRegistrationsCommandValidator()
    {
        RuleFor(x => x.ParticipantId)
            .GreaterThan(0)
            .WithMessage("Please select a valid participant.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes cannot exceed 500 characters.");
    }
}

public class EventRow
{
    public bool IsActive { get; set; }
    public DateTime RegistrationDeadline { get; set; }
    public DateTime StartAt { get; set; }
    public int Capacity { get; set; }
}

public class CreateRegistrationsHandler
    : IRequestHandler<CreateRegistrationsCommand, RegistrationDto>
{
    private readonly IEventRegistrationDatabase _database;

    public CreateRegistrationsHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<RegistrationDto> Handle(
        CreateRegistrationsCommand request,
        CancellationToken cancellationToken)
    {
        var  selectSql = $@"
            {RegistrationQueries.Select}
            WHERE r.Id = @Id;";

        using var connection = _database.Open();
        using var tx = connection.BeginTransaction();

        request.Notes = request.Notes?.Trim();

        try
        {
            
            var ev = await connection.QuerySingleOrDefaultAsync<EventRow>(
              new CommandDefinition(
                @"SELECT IsActive, RegistrationDeadline, StartAt, Capacity
                  FROM Events
                  WHERE Id = @EventId
                  FOR UPDATE;",
                new { request.EventId },
                transaction: tx ,
                cancellationToken: cancellationToken
                )

                );

            if (ev is null)
                throw new NotFoundException($"Event with ID {request.EventId} was not found.");
            if (!ev.IsActive)
                throw new BusinessException("The event is inactive.");

            
            var participantIsActive = await connection.ExecuteScalarAsync<bool?>(
               new CommandDefinition(
                "SELECT IsActive FROM Participants WHERE Id = @ParticipantId;",
                new { request.ParticipantId }, 
                transaction: tx ,
                cancellationToken: cancellationToken
                 )
                );

            if (participantIsActive is null)
                throw new NotFoundException($"Participant with ID {request.ParticipantId} was not found.");
            if (!participantIsActive.Value)
                throw new BusinessException("The selected participant is inactive.");

            
            var now = DateTime.UtcNow;

            if (now >= ev.RegistrationDeadline)
                throw new BusinessException("The registration deadline has passed.");
            if (now >= ev.StartAt)
                throw new BusinessException("The event has already started.");


            var existing = await connection.QuerySingleOrDefaultAsync<ExistingRegistration>(
               new CommandDefinition(
                @"SELECT Id, Status
                  FROM Registrations
                  WHERE EventId = @EventId AND ParticipantId = @ParticipantId;",
                new { request.EventId, request.ParticipantId }, 
                transaction:tx ,
                cancellationToken: cancellationToken
                )
                );

            if (existing is not null && existing.Status == 1)
                throw new BusinessException("This participant is already registered for this event.");

           
            var activeCount = await connection.ExecuteScalarAsync<int>(
               new CommandDefinition(
                "SELECT COUNT(*) FROM Registrations WHERE EventId = @EventId AND Status = 1;",
                new { request.EventId },
                transaction: tx ,
                cancellationToken: cancellationToken
                )
                );

            if (activeCount >= ev.Capacity)
                throw new BusinessException("The event is full.");

            
            long registrationId;

            if (existing is not null)
            {
                await connection.ExecuteAsync(
                  new CommandDefinition(
                    @"UPDATE Registrations
                      SET Status = 1, Notes = @Notes, CancelledAt = NULL
                      WHERE Id = @Id;",
                    new { existing.Id, request.Notes },
                    transaction : tx ,
                    cancellationToken: cancellationToken
                    )
                   );

                registrationId = existing.Id;
            }
            else
            {
                await connection.ExecuteAsync(
                   new CommandDefinition(
                    @"INSERT INTO Registrations
                          (EventId, ParticipantId, Status, Notes, RegisteredAt, CancelledAt)
                      VALUES
                          (@EventId, @ParticipantId, 1, @Notes, UTC_TIMESTAMP(), NULL);",
                    new { request.EventId, request.ParticipantId, request.Notes },
                    transaction : tx ,
                    cancellationToken: cancellationToken
                     )
                    );

                registrationId = await connection.ExecuteScalarAsync<long>(
                new CommandDefinition(
                    "SELECT LAST_INSERT_ID();",
                    transaction: tx ,
                    cancellationToken: cancellationToken
                )
                );
            }

           
            var result = await connection.QuerySingleAsync<RegistrationDto>(
               new CommandDefinition(
                selectSql,
                new { Id = registrationId },
                transaction : tx , 
                cancellationToken: cancellationToken)
            );

            tx.Commit();
            return result;
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            throw new DuplicateResourceException(
                "This participant is already registered for this event.");
        }
    }
}