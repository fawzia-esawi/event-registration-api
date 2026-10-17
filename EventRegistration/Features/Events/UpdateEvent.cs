using Dapper;
using EventRegistration.Api.Interfaces;
using MediatR;
using FluentValidation;
using EventRegistration.Api.Exceptions;
namespace EventRegistration.Api.Features.Events;

public class UpdateEventCommand : IRequest<long>{
    public long Id { get; set; }
    public string Name {get;set;}= string.Empty;
    public int CategoryId{get;set;} 
    public string? Description {get;set;}
    public string Location {get;set;}= string.Empty;
    public int Capacity {get;set;} 
    public DateTime  StartAt {get;set;} 
    public DateTime  EndAt {get;set;}
    public DateTime  RegistrationDeadline {get;set;}
    public bool  IsActive {get;set;}  = true ;
}
public class UpdateEventCommandValidator
    : AbstractValidator<UpdateEventCommand>{
     public UpdateEventCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);

        RuleFor(x => x.CategoryId)
            .GreaterThan(0);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        RuleFor(x => x.Location)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.EndAt)
            .GreaterThan(x => x.StartAt)
            .WithMessage("EndAt must be after StartAt.");

        RuleFor(x => x.RegistrationDeadline)
            .LessThanOrEqualTo(x => x.StartAt)
            .WithMessage(
                "RegistrationDeadline must be before or equal to StartAt."
            );

        RuleFor(x => x.Capacity)
            .InclusiveBetween(1, 10000);
    }
}



public class UpdateEventHandler
    : IRequestHandler<UpdateEventCommand, long>
{
    private readonly IEventRegistrationDatabase _database;

    public UpdateEventHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<long> Handle(
        UpdateEventCommand request,
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

        // 2. Check whether the selected category exists and is active
        const string checkCategorySql = @"
            SELECT EXISTS(
                SELECT 1
                FROM Categories
                WHERE Id = @CategoryId
                  AND IsActive = 1
            );
        ";

        var categoryExists = await connection.ExecuteScalarAsync<bool>(
            checkCategorySql,
            new { request.CategoryId }
        );

        if (!categoryExists)
        {
            throw new BusinessException(
              "The selected category does not exist or is inactive."
               );
        }

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

        await connection.ExecuteAsync(updateSql, request);

        return request.Id;
    }
}
