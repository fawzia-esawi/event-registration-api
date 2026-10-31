using EventRegistration.Api.Interfaces;
using EventRegistration.Api.Exceptions;
using MediatR;
using Dapper;
using FluentValidation;
using System.Text;
using MySqlConnector;
namespace EventRegistration.Api.Features.Registrations;


public class CreateRegistrationsCommand: IRequest<RegistrationDetailsDto>
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
    : IRequestHandler<CreateRegistrationsCommand, RegistrationDetailsDto>
{
    private readonly IEventRegistrationDatabase _database;

    public CreateRegistrationsHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<RegistrationDetailsDto> Handle(
        CreateRegistrationsCommand request,
        CancellationToken cancellationToken)
    {
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

        using var connection = _database.Open();
        using var tx = connection.BeginTransaction();

        try
        {
            
            var ev = await connection.QuerySingleOrDefaultAsync<EventRow>(
                @"SELECT IsActive, RegistrationDeadline, StartAt, Capacity
                  FROM Events
                  WHERE Id = @EventId
                  FOR UPDATE;",
                new { request.EventId }, tx);

            if (ev is null)
                throw new NotFoundException($"Event with ID {request.EventId} was not found.");
            if (!ev.IsActive)
                throw new BusinessException("The event is inactive.");

            
            var participantIsActive = await connection.ExecuteScalarAsync<bool?>(
                "SELECT IsActive FROM Participants WHERE Id = @ParticipantId;",
                new { request.ParticipantId }, tx);

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
                @"SELECT Id, Status
                  FROM Registrations
                  WHERE EventId = @EventId AND ParticipantId = @ParticipantId;",
                new { request.EventId, request.ParticipantId }, tx);

            if (existing is not null && existing.Status == 1)
                throw new BusinessException("This participant is already registered for this event.");

           
            var activeCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Registrations WHERE EventId = @EventId AND Status = 1;",
                new { request.EventId }, tx);

            if (activeCount >= ev.Capacity)
                throw new BusinessException("The event is full.");

            
            long registrationId;

            if (existing is not null)
            {
                await connection.ExecuteAsync(
                    @"UPDATE Registrations
                      SET Status = 1, Notes = @Notes, CancelledAt = NULL
                      WHERE Id = @Id;",
                    new { existing.Id, request.Notes }, tx);

                registrationId = existing.Id;
            }
            else
            {
                await connection.ExecuteAsync(
                    @"INSERT INTO Registrations
                          (EventId, ParticipantId, Status, Notes, RegisteredAt, CancelledAt)
                      VALUES
                          (@EventId, @ParticipantId, 1, @Notes, UTC_TIMESTAMP(), NULL);",
                    new { request.EventId, request.ParticipantId, request.Notes }, tx);

                registrationId = await connection.ExecuteScalarAsync<long>(
                    "SELECT LAST_INSERT_ID();", transaction: tx);
            }

           
            var result = await connection.QuerySingleAsync<RegistrationDetailsDto>(
                selectSql, new { Id = registrationId }, tx);

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