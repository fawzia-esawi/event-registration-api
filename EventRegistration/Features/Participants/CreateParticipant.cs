using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Dapper;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using FluentValidation;
using MediatR;
using MySqlConnector;

namespace EventRegistration.Api.Features.Participants;

public class CreateParticipantCommand : IRequest<ParticipantResponse>
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? DateOfBirth { get; set; }
}

public class CreateParticipantCommandValidator : AbstractValidator<CreateParticipantCommand>
{
    private const string DateFormat = "yyyy-MM-dd";

    public CreateParticipantCommandValidator()
    {
        RuleFor(x => x.FullName)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Full name is required.")
            .Must(value => value!.Trim().Length <= 150).WithMessage("Full name must not exceed 150 characters.");

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Email is required.")
            .Must(value => value!.Trim().Length <= 255).WithMessage("Email must not exceed 255 characters.")
            .Must(value => new EmailAddressAttribute().IsValid(value!.Trim())).WithMessage("Email is not valid.");

        RuleFor(x => x.Phone)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Phone is required.")
            .Must(value => value!.Trim().Length <= 30).WithMessage("Phone must not exceed 30 characters.");

        RuleFor(x => x.DateOfBirth)
            .Cascade(CascadeMode.Stop)
            .Must(value => DateOnly.TryParseExact(value!.Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                .WithMessage("Date of birth must be a valid date (yyyy-MM-dd).")
            .Must(value => DateOnly.ParseExact(value!.Trim(), DateFormat, CultureInfo.InvariantCulture) <= DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("Date of birth cannot be in the future.")
            .When(x => !string.IsNullOrWhiteSpace(x.DateOfBirth));
    }
}

public class CreateParticipantHandler : IRequestHandler<CreateParticipantCommand, ParticipantResponse>
{
    private readonly IEventRegistrationDatabase _database;

    public CreateParticipantHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<ParticipantResponse> Handle(
        CreateParticipantCommand request,
        CancellationToken cancellationToken)
    {
        var fullName = request.FullName!.Trim();
        var email = request.Email!.Trim();
        var phone = request.Phone!.Trim();

        DateTime? dateOfBirth = null;
        if (!string.IsNullOrWhiteSpace(request.DateOfBirth))
        {
            dateOfBirth = DateOnly
                .ParseExact(request.DateOfBirth.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture)
                .ToDateTime(TimeOnly.MinValue);
        }

        using var connection = _database.Open();

        const string duplicateSql = @"
            SELECT COUNT(1)
            FROM Participants
            WHERE LOWER(TRIM(Email)) = LOWER(@Email);";

        var duplicate = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(duplicateSql, new { Email = email }, cancellationToken: cancellationToken));

        if (duplicate > 0)
            throw new DuplicateResourceException($"A participant with the email '{email}' already exists.");

        const string insertSql = @"
            INSERT INTO Participants (FullName, Email, Phone, DateOfBirth)
            VALUES (@FullName, @Email, @Phone, @DateOfBirth);
            SELECT LAST_INSERT_ID();";

        long id;
        try
        {
            id = await connection.ExecuteScalarAsync<long>(
                new CommandDefinition(
                    insertSql,
                    new { FullName = fullName, Email = email, Phone = phone, DateOfBirth = dateOfBirth },
                    cancellationToken: cancellationToken));
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            throw new DuplicateResourceException($"A participant with the email '{email}' already exists.");
        }

        const string selectSql = @"
            SELECT Id, FullName, Email, Phone,
                   DATE_FORMAT(DateOfBirth, '%Y-%m-%d') AS DateOfBirth,
                   IsActive, CreatedAt, UpdatedAt
            FROM Participants
            WHERE Id = @Id;";

        return await connection.QuerySingleAsync<ParticipantResponse>(
            new CommandDefinition(selectSql, new { Id = id }, cancellationToken: cancellationToken));
    }
}