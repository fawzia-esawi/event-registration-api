
namespace EventRegistration.Api.Features.Events;

using EventRegistration.Api.Interfaces;
using EventRegistration.Api.Exceptions;
using MediatR;
using FluentValidation;
using Dapper;
using System.Globalization;
using System.Text.RegularExpressions;

public class CreateEventCommand : IRequest<CreateResponse>
{
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string? Description { get; set; }
    public string Location { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public required  string StartAt { get; set; }
    public required  string EndAt { get; set; }
    public required  string RegistrationDeadline { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CreateResponse
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

public class CreateEventCommandValidator
    : AbstractValidator<CreateEventCommand>
{
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

    public CreateEventCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty()
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

public class CreateEventHandler : IRequestHandler<CreateEventCommand, CreateResponse>
{
    private readonly IEventRegistrationDatabase _database;

    public CreateEventHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<CreateResponse> Handle(
        CreateEventCommand request,
        CancellationToken cancellationToken)
    {
        using var connection = _database.Open();

        request.Name = request.Name.Trim();
        request.Description = request.Description?.Trim();
        request.Location = request.Location.Trim();

        const string sql = @"
            SELECT IsActive
            FROM Categories
            WHERE Id = @CategoryId;
        ";

        var categoryIsActive = await connection.QuerySingleOrDefaultAsync<bool?>(
            new CommandDefinition(
                sql,
                new { request.CategoryId },
                cancellationToken: cancellationToken
            )
        );

        if (categoryIsActive == null)
        {
            throw new NotFoundException("The selected category was not found.");
        }

        if (!categoryIsActive.Value)
        {
            throw new BusinessException("Cannot create an event under an inactive category.");
        }

        var startAt = DateTimeOffset.Parse(request.StartAt).UtcDateTime;
        var endAt = DateTimeOffset.Parse(request.EndAt).UtcDateTime;
        var registrationDeadline =
            DateTimeOffset.Parse(request.RegistrationDeadline).UtcDateTime;

        const string insertSql = @"
            INSERT INTO Events (
                CategoryId,
                Name,
                Description,
                Location,
                StartAt,
                EndAt,
                RegistrationDeadline,
                Capacity,
                IsActive
            )
            VALUES (
                @CategoryId,
                @Name,
                @Description,
                @Location,
                @StartAt,
                @EndAt,
                @RegistrationDeadline,
                @Capacity,
                @IsActive
            );

            SELECT LAST_INSERT_ID();
        ";

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
                GREATEST(e.Capacity - (
                    SELECT COUNT(*)
                    FROM Registrations r
                    WHERE r.EventId = e.Id
                    AND r.Status = 1
                ), 0) AS AvailableSeats,
                CASE
                    WHEN e.EndAt <= UTC_TIMESTAMP() THEN 'Completed'
                    WHEN e.StartAt > UTC_TIMESTAMP() THEN 'Upcoming'
                    ELSE 'Ongoing'
                END AS EventStatus,
                e.IsActive
            FROM Events e
            INNER JOIN Categories c ON e.CategoryId = c.Id
            WHERE e.Id = @Id;
        ";

        var eventId = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                insertSql,
                new
                {
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

        var createdEvent = await connection.QuerySingleAsync<CreateResponse>(
            new CommandDefinition(
                selectSql,
                new { Id = eventId },
                cancellationToken: cancellationToken
            )
        );

        return createdEvent;
    }
}
