using Dapper;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using MediatR;
using MySqlConnector;

namespace EventRegistration.Api.Features.Categories;

public record DeleteCategoryCommand(long Id) : IRequest;

public class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand>
{
    private readonly IEventRegistrationDatabase _database;

    public DeleteCategoryHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        using var connection = _database.Open();

        const string existsSql = "SELECT COUNT(1) FROM Categories WHERE Id = @Id;";

        var exists = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(existsSql, new { request.Id }, cancellationToken: cancellationToken));

        if (exists == 0)
            throw new NotFoundException($"Category with id {request.Id} was not found.");

        const string usedSql = "SELECT COUNT(1) FROM Events WHERE CategoryId = @Id;";

        var usedCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(usedSql, new { request.Id }, cancellationToken: cancellationToken));

        if (usedCount > 0)
            throw new BusinessException("This category is used by one or more events and cannot be deleted.");

        const string deleteSql = "DELETE FROM Categories WHERE Id = @Id;";

        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(deleteSql, new { request.Id }, cancellationToken: cancellationToken));
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.RowIsReferenced2)
        {
            throw new BusinessException("This category is used by one or more events and cannot be deleted.");
        }
    }
}