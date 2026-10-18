using Dapper;
using EventRegistration.Api.Interfaces;
using MediatR;

namespace EventRegistration.Api.Features.Categories
{
    public record GetCategoriesQuery(bool IncludeInactive) : IRequest<List<CategoryResponse>>;
    public class GetCategoriesHandler: IRequestHandler <GetCategoriesQuery, List<CategoryResponse>>
    {
        private readonly IEventRegistrationDatabase _database;

        public GetCategoriesHandler(IEventRegistrationDatabase database)
        {
            _database = database;
        }
        public async Task<List<CategoryResponse>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
        {
            const string sql = @"
            SELECT Id, Name, Description, IsActive, CreatedAt, UpdatedAt
            FROM Categories
            WHERE (@IncludeInactive = 1 OR IsActive = 1)
            ORDER BY Name;";

            using var connection = _database.Open();

            var rows = await connection.QueryAsync<CategoryResponse>(
                new CommandDefinition(
                    sql,
                    new { IncludeInactive = request.IncludeInactive },
                    cancellationToken: cancellationToken));

            return rows.ToList();
        }
    }
}
    
