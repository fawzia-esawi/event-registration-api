using Dapper;
using MediatR;
using EventRegistration.Api.Interfaces;
using EventRegistration.Api.Exceptions;
using FluentValidation;
using System.Text;

public class GetEventRegistrationsQuery : IRequest<GetEventRegistrationsResult>
{
    public long EventId { get; set; } 

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public string? Search { get; set; }

    public int? Status { get; set; }
}
public class GetEventRegistrationsResult
{
    public IEnumerable<RegistrationsListItemDto> Items { get; set; }
        = Array.Empty<RegistrationsListItemDto>();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }
}

public class  RegistrationsListItemDto {
   
          public long Id { get; set; } 

          public long EventId { get; set; } 

          public string EventName { get; set; } 

          public long ParticipantId { get; set; } 

          public string ParticipantName { get; set; }

          public string ParticipantEmail { get; set; }
        
          public string ParticipantPhone { get; set; }

          public int Status { get; set; }

          public string StatusName { get; set; }

          public string? Notes { get; set; }

          public DateTime RegisteredAt { get; set; }

          public DateTime? CancelledAt { get; set; }
}

public class GetEventRegistrationsQueryValidator
    : AbstractValidator<GetEventRegistrationsQuery>
{
    public GetEventRegistrationsQueryValidator()
    {
        RuleFor(x => x.EventId)
            .GreaterThan(0)
            .WithMessage("EventId must be greater than 0.");
        
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");
        
        RuleFor(x => x.Status)
            .Must(status => status == null || status == 1 || status == 2)
            .WithMessage("Status must be 1 (Active) or 2 (Cancelled).");

    }
}

public class GetEventRegistrationsHandler
    : IRequestHandler<GetEventRegistrationsQuery, GetEventRegistrationsResult>
{
       private readonly IEventRegistrationDatabase _database;
       public GetEventRegistrationsHandler(IEventRegistrationDatabase database){
        _database=database;
       }
       public async Task<GetEventRegistrationsResult> Handle(
        GetEventRegistrationsQuery request,
        CancellationToken cancellationToken)
    {

        var where = new StringBuilder(" WHERE r.EventId = @EventId");

        var parameters = new DynamicParameters();
        parameters.Add("EventId", request.EventId);

        if(!string.IsNullOrWhiteSpace(request.Search)){
            where.Append(" AND (p.FullName LIKE @Search OR p.Email LIKE @Search OR p.Phone LIKE @Search)");
            parameters.Add("Search",$"%{request.Search.Trim()}%");
        }
        if(request.Status.HasValue){
            where.Append(" AND r.Status = @Status");
            parameters.Add("Status",request.Status.Value);
        }
        
        
        using var connection = _database.Open();

        const string checkEventSql = @"
           SELECT COUNT(*)
           FROM Events
           WHERE Id = @EventId;
        ";

        var eventExists = await connection.ExecuteScalarAsync<int>(
           checkEventSql,
           new { request.EventId }
        );


        if (eventExists == 0)
          {
             throw new NotFoundException(
               $"Event with ID {request.EventId} was not found."
               );
          }



        var countSql = $@"
        SELECT COUNT(*)
        FROM Registrations r
        INNER JOIN Participants p ON p.Id = r.ParticipantId
        {where};
        ";

        var totalCount = await connection.ExecuteScalarAsync<int>(
            countSql,
            parameters
        );

        var offset = (request.Page - 1) * request.PageSize;
        
        parameters.Add("PageSize",request.PageSize);
        parameters.Add("Offset",offset);
        

        var registrationsSql = $@"
        SELECT 
          r.Id AS Id,
          r.EventId AS EventId,
          e.Name AS EventName,
          r.ParticipantId AS ParticipantId,
          p.FullName  AS ParticipantName,
          p.Email AS ParticipantEmail,
          p.Phone AS ParticipantPhone,
          r.Status AS Status,
          CASE
             WHEN r.Status = 1 THEN 'Active'
             WHEN r.Status = 2 THEN 'Cancelled'
             ELSE 'Unknown'
          END AS StatusName,
          r.Notes AS Notes,
          r.RegisteredAt AS RegisteredAt,
          r.CancelledAt AS CancelledAt
        FROM Registrations r
        INNER JOIN Participants p ON p.Id = r.ParticipantId
        INNER JOIN Events e ON e.Id = r.EventId
        {where}
        ORDER BY r.RegisteredAt DESC, r.Id DESC
        LIMIT @PageSize OFFSET @Offset;
        ";
        
        var items = await connection.QueryAsync<RegistrationsListItemDto>(registrationsSql,parameters);
        
        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
        
        return new GetEventRegistrationsResult
        {
           Items = items,
           Page = request.Page,
           PageSize = request.PageSize,
           TotalCount = totalCount,
           TotalPages = totalPages
        };

    }

}



