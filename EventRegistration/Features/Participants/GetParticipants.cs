using Dapper;
using EventRegistration.Api.Interfaces;
using FluentValidation;
using MediatR;

namespace EventRegistration.Api.Features.Participants;

public class GetParticipantsQuery : IRequest<GetParticipantsResult>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}

public class ParticipantResponse
{
    public long Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? DateOfBirth { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class GetParticipantsResult
{
    public IReadOnlyList<ParticipantResponse> Items { get; set; } = Array.Empty<ParticipantResponse>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class GetParticipantsQueryValidator : AbstractValidator<GetParticipantsQuery>
{
    public GetParticipantsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x.Search)
            .MaximumLength(100).WithMessage("Search must not exceed 100 characters.");
    }
}

public class GetParticipantsHandler : IRequestHandler<GetParticipantsQuery, GetParticipantsResult>
{
    private readonly IEventRegistrationDatabase _database;

    public GetParticipantsHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<GetParticipantsResult> Handle(
        GetParticipantsQuery request,
        CancellationToken cancellationToken)
    {
        var offset = (request.Page - 1) * request.PageSize;

        var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        var searchPattern = search is null ? null : $"%{EscapeLike(search)}%";

        const string countSql = @"
            SELECT COUNT(*)
            FROM Participants
            WHERE (@Search IS NULL
                   OR FullName LIKE @SearchPattern
                   OR Email LIKE @SearchPattern
                   OR Phone LIKE @SearchPattern)
              AND (@IsActive IS NULL OR IsActive = @IsActive);";

        const string listSql = @"
            SELECT Id, FullName, Email, Phone,
                   DATE_FORMAT(DateOfBirth, '%Y-%m-%d') AS DateOfBirth,
                   IsActive, CreatedAt, UpdatedAt
            FROM Participants
            WHERE (@Search IS NULL
                   OR FullName LIKE @SearchPattern
                   OR Email LIKE @SearchPattern
                   OR Phone LIKE @SearchPattern)
              AND (@IsActive IS NULL OR IsActive = @IsActive)
            ORDER BY FullName, Id
            LIMIT @PageSize OFFSET @Offset;";

        var parameters = new
        {
            Search = search,
            SearchPattern = searchPattern,
            request.IsActive,
            request.PageSize,
            Offset = offset
        };

        using var connection = _database.Open();

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        var items = await connection.QueryAsync<ParticipantResponse>(
            new CommandDefinition(listSql, parameters, cancellationToken: cancellationToken));

        return new GetParticipantsResult
        {
            Items = items.ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}