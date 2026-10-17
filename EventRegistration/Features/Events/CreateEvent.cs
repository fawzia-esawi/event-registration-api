namespace EventRegistration.Api.Features.Events;
using EventRegistration.Api.Interfaces;
using EventRegistration.Api.Exceptions;
using MediatR;
using FluentValidation;
using Dapper; 



public class CreateEventCommand : IRequest<long>{
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
public class CreateEventCommandValidator
    : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
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
public class CreateEventHandler:IRequestHandler<CreateEventCommand,long>
{
private readonly IEventRegistrationDatabase _database;
public CreateEventHandler(IEventRegistrationDatabase database){
    _database=database;
}
public  async Task<long> Handle(CreateEventCommand request,CancellationToken cancellationToken)
    {
        using var connection = _database.Open();

        const string sql= @"
        SELECT EXISTS(
        SELECT 1
        FROM Categories c
        WHERE c.Id = @CategoryId
          AND c.IsActive = 1);
        ";
        var categoryExists= await connection.ExecuteScalarAsync<bool>(sql,new { request.CategoryId });
        if (!categoryExists)
        {
            throw new BusinessException(
                "The selected category does not exist or is inactive."
                  );
        }
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
    var eventId=await connection.ExecuteScalarAsync<long>(insertSql,request);
    return eventId;
    }
}
 