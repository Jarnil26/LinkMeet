using LinkMeet.Domain.Enums;

namespace LinkMeet.Domain.Entities;

public class Participant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MeetingId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    public bool IsAudioOn { get; set; } = true;
    public bool IsVideoOn { get; set; } = true;
    public ParticipantRole Role { get; set; } = ParticipantRole.Participant;

    // Navigation
    public Meeting Meeting { get; set; } = null!;
    public User User { get; set; } = null!;
}
