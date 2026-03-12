using LinkMeet.Domain.Enums;

namespace LinkMeet.Domain.Entities;

public class Meeting
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string MeetingCode { get; set; } = GenerateCode();
    public Guid HostId { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public MeetingStatus Status { get; set; } = MeetingStatus.Scheduled;
    public string? Password { get; set; }
    public bool HasWaitingRoom { get; set; } = false;
    public bool IsLocked { get; set; } = false;

    // Navigation
    public User Host { get; set; } = null!;
    public ICollection<Participant> Participants { get; set; } = new List<Participant>();
    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    private static string GenerateCode()
    {
        var random = new Random();
        return $"{random.Next(100, 999)}-{random.Next(100, 999)}-{random.Next(1000, 9999)}";
    }
}
