using Dapper;
using MediatR;
using EventRegistration.Api.Interfaces;
using EventRegistration.Api.Exceptions;
using FluentValidation;
using EventRegistration.Api.Common.Registrations;
namespace EventRegistration.Api.Features.Registrations;

public class GetRegistrationByIdQuery
    : IRequest<RegistrationDto>
{
    public long Id { get; set; }
}

public class GetRegistrationByIdQueryValidator
    : AbstractValidator<GetRegistrationByIdQuery>
{
    public GetRegistrationByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Registration ID must be greater than 0.");
    }
}

public class GetRegistrationByIdHandler
    : IRequestHandler<GetRegistrationByIdQuery, RegistrationDto>
{
    private readonly IEventRegistrationDatabase _database;

    public GetRegistrationByIdHandler(
        IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<RegistrationDto> Handle(
        GetRegistrationByIdQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = _database.Open();

        var  sql = $@"
        {RegistrationQueries.Select}
        WHERE r.Id = @Id;
        ";

        var registration =
            await connection.QuerySingleOrDefaultAsync<RegistrationDto>(
                new CommandDefinition(
                    sql,
                    new { request.Id },
                    cancellationToken: cancellationToken
                )
            );

        if (registration is null)
        {
            throw new NotFoundException(
                $"Registration with ID {request.Id} was not found."
            );
        }

        return registration;
    }
}