using LinkMeet.Domain.Enums;

namespace LinkMeet.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Participant;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Meeting> HostedMeetings { get; set; } = new List<Meeting>();
    public ICollection<Participant> Participations { get; set; } = new List<Participant>();
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
