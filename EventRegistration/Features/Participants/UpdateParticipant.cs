using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Dapper;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using FluentValidation;
using MediatR;
using MySqlConnector;

namespace EventRegistration.Api.Features.Participants;

public record UpdateParticipantRequest(
    string? FullName,
    string? Email,
    string? Phone,
    string? DateOfBirth,
    bool? IsActive);

public record UpdateParticipantCommand(
    long Id,
    string? FullName,
    string? Email,
    string? Phone,
    string? DateOfBirth,
    bool? IsActive) : IRequest<ParticipantResponse>;

public class UpdateParticipantCommandValidator : AbstractValidator<UpdateParticipantCommand>
{
    private const string DateFormat = "yyyy-MM-dd";

    public UpdateParticipantCommandValidator()
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

        RuleFor(x => x.IsActive)
            .NotNull().WithMessage("IsActive is required.");
    }
}

public class UpdateParticipantHandler : IRequestHandler<UpdateParticipantCommand, ParticipantResponse>
{
    private readonly IEventRegistrationDatabase _database;

    public UpdateParticipantHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task<ParticipantResponse> Handle(
        UpdateParticipantCommand request,
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

        const string existsSql = "SELECT COUNT(1) FROM Participants WHERE Id = @Id;";

        var exists = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(existsSql, new { request.Id }, cancellationToken: cancellationToken));

        if (exists == 0)
            throw new NotFoundException($"Participant with id {request.Id} was not found.");

        const string duplicateSql = @"
            SELECT COUNT(1)
            FROM Participants
            WHERE LOWER(TRIM(Email)) = LOWER(@Email) AND Id <> @Id;";

        var duplicate = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(duplicateSql, new { Email = email, request.Id }, cancellationToken: cancellationToken));

        if (duplicate > 0)
            throw new DuplicateResourceException($"A participant with the email '{email}' already exists.");

        const string updateSql = @"
            UPDATE Participants
            SET FullName = @FullName,
                Email = @Email,
                Phone = @Phone,
                DateOfBirth = @DateOfBirth,
                IsActive = @IsActive,
                UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id;";

        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    updateSql,
                    new
                    {
                        FullName = fullName,
                        Email = email,
                        Phone = phone,
                        DateOfBirth = dateOfBirth,
                        IsActive = request.IsActive,
                        request.Id
                    },
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
            new CommandDefinition(selectSql, new { request.Id }, cancellationToken: cancellationToken));
    }
}