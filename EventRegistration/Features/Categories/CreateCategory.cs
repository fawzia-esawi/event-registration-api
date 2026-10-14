using Dapper;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using FluentValidation;
using MediatR;
using MySqlConnector;

namespace EventRegistration.Api.Features.Categories;

public record CreateCategoryCommand(string? Name, string? Description) : IRequest<CategoryResponse>;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Name is required.")
            .Must(name => name!.Trim().Length >= 2).WithMessage("Name must be at least 2 characters.")
            .Must(name => name!.Trim().Length <= 100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .Must(description => description is null || description.Trim().Length <= 500)
            .WithMessage("Description must not exceed 500 characters.");
    }
}

public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, CategoryResponse>
{
    private readonly IEventRegistrationDatabase _database;

    public CreateCategoryHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<CategoryResponse> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var name = request.Name!.Trim();
        var description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();

        using var connection = _database.Open();

        const string existsSql = @"
            SELECT COUNT(1)
            FROM Categories
            WHERE LOWER(Name) = LOWER(@Name);";

        var exists = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(existsSql, new { Name = name }, cancellationToken: cancellationToken));

        if (exists > 0)
            throw new DuplicateResourceException($"A category named '{name}' already exists.");

        const string insertSql = @"
            INSERT INTO Categories (Name, Description)
            VALUES (@Name, @Description);
            SELECT LAST_INSERT_ID();";

        long newId;
        try
        {
            newId = await connection.ExecuteScalarAsync<long>(
                new CommandDefinition(
                    insertSql,
                    new { Name = name, Description = description },
                    cancellationToken: cancellationToken));
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            throw new DuplicateResourceException($"A category named '{name}' already exists.");
        }

        const string selectSql = @"
            SELECT Id, Name, Description, IsActive, CreatedAt, UpdatedAt
            FROM Categories
            WHERE Id = @Id;";

        return await connection.QuerySingleAsync<CategoryResponse>(
            new CommandDefinition(selectSql, new { Id = newId }, cancellationToken: cancellationToken));
    }
}