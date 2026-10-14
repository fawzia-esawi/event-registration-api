using Dapper;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using MediatR;

namespace EventRegistration.Api.Features.Categories;

public record GetCategoryByIdQuery(long Id) : IRequest<CategoryResponse>;

public class GetCategoryByIdHandler : IRequestHandler<GetCategoryByIdQuery, CategoryResponse>
{
    private readonly IEventRegistrationDatabase _database;

    public GetCategoryByIdHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<CategoryResponse> Handle(
        GetCategoryByIdQuery request,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT Id, Name, Description, IsActive, CreatedAt, UpdatedAt
            FROM Categories
            WHERE Id = @Id;";

        using var connection = _database.Open();

        var category = await connection.QuerySingleOrDefaultAsync<CategoryResponse>(
            new CommandDefinition(sql, new { request.Id }, cancellationToken: cancellationToken));

        return category
            ?? throw new NotFoundException($"Category with id {request.Id} was not found.");
    }
}