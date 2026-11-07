namespace EventRegistration.Api.Common.Registrations;
public static class RegistrationQueries
{
    public const string Select = @"
        SELECT
            r.Id AS Id,
            r.EventId AS EventId,
            e.Name AS EventName,
            r.ParticipantId AS ParticipantId,
            p.FullName AS ParticipantName,
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
    ";
}