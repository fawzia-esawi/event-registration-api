namespace EventRegistration.Api.Common.Registrations;
public class RegistrationDto
{
    public long Id { get; set; }
    public long EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public long ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public string ParticipantPhone { get; set; } = string.Empty;
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}