using Dapper;
using EventRegistration.Api.Interfaces;
using MediatR;
using FluentValidation;
using EventRegistration.Api.Exceptions;
using System.Globalization;
using System.Text.RegularExpressions;
namespace EventRegistration.Api.Features.Events;

public class UpdateEventCommand : IRequest<UpdateResponse>{
    public long Id { get; set; }
    public string Name {get;set;}= string.Empty;
    public int CategoryId{get;set;} 
    public string? Description {get;set;}
    public string Location {get;set;}= string.Empty;
    public int Capacity {get;set;} 
    public required  string   StartAt {get;set;} 
    public required  string   EndAt {get;set;}
    public required  string   RegistrationDeadline {get;set;}
    public bool?  IsActive {get;set;} 
}

public class UpdateResponse
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public long CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public int Capacity { get; set; }

    public int ActiveRegistrationCount { get; set; }

    public int AvailableSeats { get; set; }

    public string EventStatus { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}

public class UpdateEventCommandValidator
    : AbstractValidator<UpdateEventCommand>{
        private bool IsValidUtcDate(string value)
{
    return !string.IsNullOrWhiteSpace(value)
        && Regex.IsMatch(value, @"(Z|[+-]\d{2}:\d{2})$")
        && DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _
        );
}
     public UpdateEventCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);

        RuleFor(x => x.CategoryId)
            .GreaterThan(0);

        RuleFor(x => x.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Name is required.")
            .MaximumLength(150);

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        RuleFor(x => x.Location)
            .Must(location => !string.IsNullOrWhiteSpace(location))
            .WithMessage("Location is required.")
            .MaximumLength(200);

        RuleFor(x => x.StartAt)
            .NotEmpty()
            .WithMessage("StartAt is required.")
            .Must(IsValidUtcDate)
            .WithMessage("StartAt must be a valid ISO 8601 date with Z or UTC offset.");
        

        RuleFor(x => x.EndAt)
            .NotEmpty()
            .WithMessage("EndAt is required.")
            .Must(IsValidUtcDate)
            .WithMessage("EndAt must be a valid ISO 8601 date with Z or UTC offset.");

        RuleFor(x => x.RegistrationDeadline)
            .NotEmpty()
            .WithMessage("RegistrationDeadline is required.")
            .Must(IsValidUtcDate)
            .WithMessage("RegistrationDeadline must be a valid ISO 8601 date with Z or UTC offset.");
            
        RuleFor(x => x)
            .Must(x =>
            IsValidUtcDate(x.StartAt) &&
            IsValidUtcDate(x.EndAt) &&
            DateTimeOffset.Parse(x.EndAt) > DateTimeOffset.Parse(x.StartAt))
           .WithMessage("EndAt must be after StartAt.");


        RuleFor(x => x)
            .Must(x =>
            IsValidUtcDate(x.StartAt) &&
            IsValidUtcDate(x.RegistrationDeadline) &&
            DateTimeOffset.Parse(x.RegistrationDeadline) <= DateTimeOffset.Parse(x.StartAt))
            .WithMessage("RegistrationDeadline must be before or equal to StartAt.");

        RuleFor(x => x.Capacity)
            .NotEmpty()
            .WithMessage("Capacity is required.")
            .InclusiveBetween(1, 10000);
        RuleFor(x => x.IsActive)
            .NotNull()
            .WithMessage("IsActive is required.");
    }
}



public class UpdateEventHandler
    : IRequestHandler<UpdateEventCommand, UpdateResponse>
{
    private readonly IEventRegistrationDatabase _database;

    public UpdateEventHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<UpdateResponse> Handle(
        UpdateEventCommand request,
        CancellationToken cancellationToken)
    {
        using var connection = _database.Open();

        request.Name = request.Name.Trim();
        request.Description = request.Description?.Trim();
        request.Location = request.Location.Trim();

        const string checkEventSql = @"
            SELECT EXISTS(
                SELECT 1
                FROM Events
                WHERE Id = @Id
            );
        ";

        var eventExists = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                checkEventSql,
                new { request.Id },
                cancellationToken: cancellationToken
            )
        );

        if (!eventExists)
        {
            throw new NotFoundException(
                $"Event with ID {request.Id} was not found."
              );
        }
        const string activeRegistrationsSql = @"
                   SELECT COUNT(*)
                   FROM Registrations
                   WHERE EventId = @Id
                   AND Status = 1;
                   ";

        var activeRegistrationCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                activeRegistrationsSql,
                new { request.Id },
                cancellationToken: cancellationToken
            )

        );

          if (request.Capacity < activeRegistrationCount)
          {
              throw new BusinessException(
             $"Capacity cannot be less than the number of active registrations ({activeRegistrationCount})."
             );
          }
        // 2. Check whether the selected category exists 
        const string checkCategorySql = @"
            SELECT EXISTS(
                SELECT 1
                FROM Categories
                WHERE Id = @CategoryId
                
            );
        ";

        var categoryExists = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                checkCategorySql,
                new { request.CategoryId },
                cancellationToken: cancellationToken
            )
        );

        if (!categoryExists)
        {
              throw new NotFoundException("The selected category was not found.");
        }


        var startAt = DateTimeOffset.Parse(request.StartAt).UtcDateTime;
        var endAt = DateTimeOffset.Parse(request.EndAt).UtcDateTime;
        var registrationDeadline =
            DateTimeOffset.Parse(request.RegistrationDeadline).UtcDateTime;
       
       
        const string updateSql = @"
            UPDATE Events
            SET
                CategoryId = @CategoryId,
                Name = @Name,
                Description = @Description,
                Location = @Location,
                StartAt = @StartAt,
                EndAt = @EndAt,
                RegistrationDeadline = @RegistrationDeadline,
                Capacity = @Capacity,
                IsActive = @IsActive,
                UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id;
        ";

        await connection.ExecuteAsync(
            new CommandDefinition(
                updateSql,
                new
                {
                    request.Id,
                    request.CategoryId,
                    request.Name,
                    request.Description,
                    request.Location,
                    StartAt = startAt,
                    EndAt = endAt,
                    RegistrationDeadline = registrationDeadline,
                    request.Capacity,
                    request.IsActive
                },
                cancellationToken: cancellationToken
            )
        );
        
        const string selectSql = @"
        SELECT
           e.Id,
           e.Name,
           e.CategoryId,
           c.Name AS CategoryName,
           e.Location,
           e.StartAt,
           e.EndAt,
           e.Capacity,
           (
                SELECT COUNT(*)
                FROM Registrations r
                WHERE r.EventId = e.Id
                AND r.Status = 1
            ) AS ActiveRegistrationCount,
           GREATEST(
                   e.Capacity - (
                   SELECT COUNT(*)
                   FROM Registrations r
                   WHERE r.EventId = e.Id
                  AND r.Status = 1
                   ), 0
             ) AS AvailableSeats,
            CASE
                 WHEN e.EndAt <= UTC_TIMESTAMP() THEN 'Completed'
                 WHEN e.StartAt > UTC_TIMESTAMP() THEN 'Upcoming'
                 ELSE 'Ongoing'
                 END AS EventStatus,
            e.IsActive
        FROM Events e
        INNER JOIN Categories c ON e.CategoryId = c.Id
        WHERE e.Id = @Id;";

        var updatedEvent = await connection.QuerySingleAsync<UpdateResponse>(
            new CommandDefinition(
                selectSql,
                new { request.Id },
                cancellationToken: cancellationToken
            )
        );

        return updatedEvent;
    }
}
