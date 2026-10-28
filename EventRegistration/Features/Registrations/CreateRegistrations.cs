using EventRegistration.Api.Interfaces;
using EventRegistration.Api.Exceptions;
using MediatR;
using Dapper;
using FluentValidation;
using System.Text;



public class CreateRegistrationsCommand: IRequest<RegistrationResult>
{
    public long EventId { get; set; } 
    public long ParticipantId { get; set; }  
    public string? Notes {get;set;}
}

public class RegistrationResult
{
    public long Id { get; set; }
    public string Message { get; set; } = string.Empty;
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

public class CreateRegistrationsHandler: IRequestHandler<CreateRegistrationsCommand, RegistrationResult>
{
private readonly IEventRegistrationDatabase _database;
 public CreateRegistrationsHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

public async Task<RegistrationResult>  Handle( CreateRegistrationsCommand request , CancellationToken cancellationToken ){
        using var connection = _database.Open();

         var existEventSql=$@"
         SELECT EXISTS(
         SELECT 1
         FROM Events e
         WHERE e.Id=@EventId 
         AND e.IsActive = 1
         );
         ";
        var existEvent= await connection.ExecuteScalarAsync<bool>(existEventSql,new { request.EventId });
        if(!existEvent){
            throw new BusinessException(
                "The Event does not exist or is inactive."
                  );
        }

         var existParticipantSql=$@"
         SELECT EXISTS(
         SELECT 1
         FROM Participants p
         WHERE p.Id=@ParticipantId 
         AND p.IsActive = 1
         );
         ";
        var existParticipant= await connection.ExecuteScalarAsync<bool>(existParticipantSql,new { request.ParticipantId });
        if(!existParticipant){
            throw new BusinessException(
                "The selected Participant does not exist or is inactive."
                  );
        }

        var eventDatesSql = @"
          SELECT RegistrationDeadline, StartAt
          FROM Events
          WHERE Id = @EventId;
        "; 

       var eventDates = await connection.QuerySingleAsync<(DateTime RegistrationDeadline,DateTime StartAt)>(
           eventDatesSql,
            new { request.EventId }
        );

        var now = DateTime.UtcNow;

        if (now >= eventDates.RegistrationDeadline)
        {
            throw new BusinessException(
            "The registration deadline has passed."
            );
        }

        if (now >= eventDates.StartAt){
           throw new BusinessException(
          "The event has already started."
          );
        }
        
        var existingRegistration = await connection.QuerySingleOrDefaultAsync<ExistingRegistration>(
         @"
         SELECT Id, Status
         FROM Registrations
         WHERE EventId = @EventId
          AND ParticipantId = @ParticipantId;
          ",new{request.EventId,request.ParticipantId});


          if (existingRegistration is not null &&
              existingRegistration.Status == 1)
           {
              throw new BusinessException(
               "This participant is already registered for this event." );
           }

           var capacitySql = @"
               SELECT Capacity
               FROM Events e
               WHERE e.Id = @EventId;
                ";

            var capacity = await connection.ExecuteScalarAsync<int>(
                  capacitySql,
                  new { request.EventId }
                 );

            var activeCountSql = @"
                SELECT COUNT(*)
                FROM Registrations
                WHERE EventId = @EventId
                AND Status = 1;
                 ";

            var activeCount = await connection.ExecuteScalarAsync<int>(
                activeCountSql,
                new { request.EventId }
                );

            if (existingRegistration is null && activeCount >= capacity)
            {
                 throw new BusinessException("The event is full.");
            }

            if (existingRegistration?.Status == 2 && activeCount >= capacity)
            {
                throw new BusinessException("The event is full.");
            }
////////////////////////////////////////////////////////////////////////////////////////////////////
            long registrationId; 
            
            if (existingRegistration is not null)
            {
                await connection.ExecuteAsync(
                  @"
                  UPDATE Registrations
                  SET Status = 1,
                  Notes = @Notes,
                  CancelledAt = NULL
                  WHERE Id = @Id;
                  ",new{
                    existingRegistration.Id,
                    request.Notes
                    });
                    registrationId = existingRegistration.Id;
            }
           else
           {await connection.ExecuteAsync(

           @"
           INSERT INTO Registrations
               (EventId, ParticipantId, Status, Notes, RegisteredAt, CancelledAt)
           VALUES
               (@EventId, @ParticipantId, @Status, @Notes, UTC_TIMESTAMP(), NULL);
           ",new{
            request.EventId,
            request.ParticipantId,
            Status = 1,
            request.Notes
            });
            

            registrationId = await connection.ExecuteScalarAsync<long>( "SELECT LAST_INSERT_ID();");
            }   


return new RegistrationResult
{
    Id = registrationId,
    Message = "Registration created successfully."
};
}
}