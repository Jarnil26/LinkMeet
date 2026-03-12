using LinkMeet.Domain.Entities;

namespace LinkMeet.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateAsync(User user);
    Task<bool> EmailExistsAsync(string email);
}

public interface IMeetingRepository
{
    Task<Meeting?> GetByIdAsync(Guid id);
    Task<Meeting?> GetByCodeAsync(string meetingCode);
    Task<List<Meeting>> GetByHostIdAsync(Guid hostId);
    Task<List<Meeting>> GetUpcomingByUserIdAsync(Guid userId);
    Task<List<Meeting>> GetPastByUserIdAsync(Guid userId);
    Task<Meeting> CreateAsync(Meeting meeting);
    Task UpdateAsync(Meeting meeting);
    Task DeleteAsync(Guid id);
}

public interface IParticipantRepository
{
    Task<Participant?> GetAsync(Guid meetingId, Guid userId);
    Task<List<Participant>> GetByMeetingIdAsync(Guid meetingId);
    Task<Participant> CreateAsync(Participant participant);
    Task UpdateAsync(Participant participant);
    Task DeleteAsync(Guid id);
}

public interface IChatMessageRepository
{
    Task<List<ChatMessage>> GetByMeetingIdAsync(Guid meetingId);
    Task<ChatMessage> CreateAsync(ChatMessage message);
}
