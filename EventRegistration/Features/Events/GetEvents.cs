namespace EventRegistration.Api.Features.Events;
using FluentValidation;
using MediatR;
using Dapper;
using EventRegistration.Api.Interfaces;
using System.Text;

public class GetEventsQuery : IRequest<GetEventsResult>
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public string? Search { get; set; }

    public long? CategoryId { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public bool? IsActive { get; set; }
}
public class GetEventsResult
{
    public IEnumerable<EventListItemDto> Items { get; set; }
        = Array.Empty<EventListItemDto>();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }
}
public class EventListItemDto
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public long CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public int Capacity { get; set; }

    public int ActiveRegistrationCount { get; set; }

    public int AvailableSeats { get; set; }

    public string EventStatus { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}

public class GetEventsQueryValidator
    : AbstractValidator<GetEventsQuery>
{
    public GetEventsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .When(x => x.CategoryId.HasValue)
            .WithMessage("CategoryId must be greater than 0.");

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(x => x.ToDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("FromDate must be before or equal to ToDate.");
    }
}
public class GetEventsHandler
    : IRequestHandler<GetEventsQuery, GetEventsResult>
{
    private readonly IEventRegistrationDatabase _database;

    public GetEventsHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<GetEventsResult> Handle(
        GetEventsQuery request,
        CancellationToken cancellationToken)
    {
        // Build the optional filters
        var where = new StringBuilder(" WHERE 1 = 1");

        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            where.Append(" AND (e.Name LIKE @Search OR e.Location LIKE @Search)");
            parameters.Add("Search", $"%{request.Search.Trim()}%");
        }

        if (request.CategoryId.HasValue)
        {
            where.Append(" AND e.CategoryId = @CategoryId");
            parameters.Add("CategoryId", request.CategoryId.Value);
        }

        if (request.FromDate.HasValue)
        {
            where.Append(" AND e.StartAt >= @FromDate");
            parameters.Add("FromDate", request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            where.Append(" AND e.StartAt <= @ToDate");
            parameters.Add("ToDate", request.ToDate.Value);
        }

        if (request.IsActive.HasValue)
        {
            where.Append(" AND e.IsActive = @IsActive");
            parameters.Add("IsActive", request.IsActive.Value);
        }

        // Open one database connection for both queries
        using var connection = _database.Open();

        // Count all events matching the filters
        var countSql = $@"
            SELECT COUNT(*)
            FROM Events e
            {where};
        ";

        var totalCount = await connection.ExecuteScalarAsync<int>(
            countSql,
            parameters
        );

        // Calculate pagination offset
        var offset = (request.Page - 1) * request.PageSize;

        // Retrieve the events for the requested page
        var eventsSql = $@"
            SELECT
                e.Id,
                e.Name,
                e.CategoryId,
                c.Name AS CategoryName,
                e.Location,
                e.StartAt,
                e.EndAt,
                e.Capacity,
                e.IsActive,
                CASE
                    WHEN e.EndAt <= UTC_TIMESTAMP() THEN 'Completed'
                    WHEN e.StartAt > UTC_TIMESTAMP() THEN 'Upcoming'
                    ELSE 'Ongoing'
                END AS EventStatus
            FROM Events e
            INNER JOIN Categories c ON e.CategoryId = c.Id
            {where}
            ORDER BY e.StartAt ASC, e.Id ASC
            LIMIT @PageSize OFFSET @Offset;
        ";

        parameters.Add("PageSize", request.PageSize);
        parameters.Add("Offset", offset);

        var items = await connection.QueryAsync<EventListItemDto>(
            eventsSql,
            parameters
        );
        var eventItems = items.ToList();

         foreach (var item in eventItems)
        {
              item.StartAt = DateTime.SpecifyKind(item.StartAt,DateTimeKind.Utc);

              item.EndAt = DateTime.SpecifyKind(item.EndAt,DateTimeKind.Utc);
        }
        // Calculate the total number of pages
        var totalPages = (int)Math.Ceiling(
            (double)totalCount / request.PageSize
        );

        // Return the result
        return new GetEventsResult
        {
            Items = eventItems,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }
}