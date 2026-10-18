using Dapper;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using FluentValidation;
using MediatR;
using MySqlConnector;

namespace EventRegistration.Api.Features.Categories;

public record UpdateCategoryRequest(string? Name, string? Description, bool? IsActive);

public record UpdateCategoryCommand(long Id, string? Name, string? Description, bool? IsActive)
    : IRequest<CategoryResponse>;

public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Name is required.")
            .Must(name => name!.Trim().Length >= 2).WithMessage("Name must be at least 2 characters.")
            .Must(name => name!.Trim().Length <= 100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .Must(description => description is null || description.Trim().Length <= 500)
            .WithMessage("Description must not exceed 500 characters.");

        RuleFor(x => x.IsActive)
            .NotNull().WithMessage("IsActive is required.");
    }
}

public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, CategoryResponse>
{
    private readonly IEventRegistrationDatabase _database;

    public UpdateCategoryHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<CategoryResponse> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var name = request.Name!.Trim();
        var description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();

        using var connection = _database.Open();

        const string existsSql = "SELECT COUNT(1) FROM Categories WHERE Id = @Id;";

        var categoryExists = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(existsSql, new { request.Id }, cancellationToken: cancellationToken));

        if (categoryExists == 0)
            throw new NotFoundException($"Category with id {request.Id} was not found.");

        const string duplicateSql = @"
            SELECT COUNT(1)
            FROM Categories
            WHERE LOWER(Name) = LOWER(@Name) AND Id <> @Id;";

        var duplicate = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(duplicateSql, new { Name = name, request.Id }, cancellationToken: cancellationToken));

        if (duplicate > 0)
            throw new DuplicateResourceException($"A category named '{name}' already exists.");

        const string updateSql = @"
            UPDATE Categories
            SET Name = @Name,
                Description = @Description,
                IsActive = @IsActive,
                UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id;";

        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    updateSql,
                    new { Name = name, Description = description, IsActive = request.IsActive, request.Id },
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
            new CommandDefinition(selectSql, new { request.Id }, cancellationToken: cancellationToken));
    }
}