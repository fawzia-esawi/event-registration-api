using Dapper;
using EventRegistration.Api.Interfaces;
using MediatR;

namespace EventRegistration.Api.Features.Dashboard;

public record GetDashboardSummaryQuery : IRequest<DashboardSummaryResponse>;

public class UpcomingEventDto
{
    public long EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public string Location { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int ActiveRegistrationCount { get; set; }
    public int AvailableSeats { get; set; }
}

public class DashboardSummaryResponse
{
    public int TotalActiveCategories { get; set; }
    public int TotalActiveParticipants { get; set; }
    public int TotalUpcomingActiveEvents { get; set; }
    public int TotalActiveRegistrations { get; set; }
    public List<UpcomingEventDto> UpcomingEvents { get; set; } = new();
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

        const string upcomingEventsSql = @"
        SELECT
          e.Id AS EventId,
          e.Name AS EventName,
          c.Name AS CategoryName,
          e.StartAt,
          e.Location,
          e.Capacity,
          COUNT(r.Id) AS ActiveRegistrationCount,
          GREATEST(e.Capacity - COUNT(r.Id), 0) AS AvailableSeats
        FROM Events e
        INNER JOIN Categories c ON c.Id = e.CategoryId
        LEFT JOIN Registrations r ON r.EventId = e.Id AND r.Status = 1
        WHERE e.IsActive = 1 AND e.StartAt > UTC_TIMESTAMP()
        GROUP BY
        e.Id, e.Name, c.Name, e.StartAt, e.Location, e.Capacity
        ORDER BY e.StartAt ASC
        LIMIT 5;";

        var summary = await connection.QuerySingleAsync<DashboardSummaryResponse>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
        
        var upcomingEvents = await connection.QueryAsync<UpcomingEventDto>(
            new CommandDefinition(upcomingEventsSql, cancellationToken: cancellationToken));    
        
        summary.UpcomingEvents = upcomingEvents.ToList();

        return summary;
    }
}