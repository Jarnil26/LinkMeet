namespace LinkMeet.Application.DTOs;

// ============ Auth DTOs ============
public class RegisterDto
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

// ============ Meeting DTOs ============
public class CreateMeetingDto
{
    public string Title { get; set; } = string.Empty;
    public DateTime? ScheduledAt { get; set; }
    public string? Password { get; set; }
    public bool HasWaitingRoom { get; set; } = false;
}

public class MeetingDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string MeetingCode { get; set; } = string.Empty;
    public Guid HostId { get; set; }
    public string HostName { get; set; } = string.Empty;
    public DateTime? ScheduledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool HasWaitingRoom { get; set; }
    public bool IsPasswordProtected { get; set; }
    public int ParticipantCount { get; set; }
}

public class JoinMeetingDto
{
    public string MeetingCode { get; set; } = string.Empty;
    public string? Password { get; set; }
}

// ============ Participant DTOs ============
public class ParticipantDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsAudioOn { get; set; }
    public bool IsVideoOn { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}

// ============ Chat DTOs ============
public class SendMessageDto
{
    public string Content { get; set; } = string.Empty;
}

public class ChatMessageDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}
