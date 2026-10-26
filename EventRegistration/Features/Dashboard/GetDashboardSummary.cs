using Dapper;
using EventRegistration.Api.Interfaces;
using MediatR;

namespace EventRegistration.Api.Features.Dashboard;

public record GetDashboardSummaryQuery : IRequest<DashboardSummaryResponse>;

public class DashboardSummaryResponse
{
    public int TotalActiveCategories { get; set; }
    public int TotalActiveParticipants { get; set; }
    public int TotalUpcomingActiveEvents { get; set; }
    public int TotalActiveRegistrations { get; set; }
}

public class GetDashboardSummaryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryResponse>
{
    private readonly IEventRegistrationDatabase _database;

    public GetDashboardSummaryHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<DashboardSummaryResponse> Handle(
        GetDashboardSummaryQuery request,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                (SELECT COUNT(*) FROM Categories WHERE IsActive = 1) AS TotalActiveCategories,
                (SELECT COUNT(*) FROM Participants WHERE IsActive = 1) AS TotalActiveParticipants,
                (SELECT COUNT(*) FROM Events WHERE IsActive = 1 AND StartAt > UTC_TIMESTAMP()) AS TotalUpcomingActiveEvents,
                (SELECT COUNT(*) FROM Registrations WHERE Status = 1) AS TotalActiveRegistrations;";

        using var connection = _database.Open();

        return await connection.QuerySingleAsync<DashboardSummaryResponse>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
    }
}