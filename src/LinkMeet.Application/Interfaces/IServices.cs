using LinkMeet.Application.DTOs;

namespace LinkMeet.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
}

public interface IMeetingService
{
    Task<MeetingDto> CreateAsync(Guid hostId, CreateMeetingDto dto);
    Task<MeetingDto?> GetByIdAsync(Guid id);
    Task<MeetingDto?> GetByCodeAsync(string code);
    Task<List<MeetingDto>> GetUpcomingAsync(Guid userId);
    Task<List<MeetingDto>> GetPastAsync(Guid userId);
    Task<MeetingDto> JoinAsync(Guid userId, JoinMeetingDto dto);
    Task CancelAsync(Guid meetingId, Guid userId);
    Task EndAsync(Guid meetingId, Guid userId);
}

public interface ITokenService
{
    string GenerateToken(Guid userId, string email, string role);
}
