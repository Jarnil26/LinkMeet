using LinkMeet.Domain.Entities;
using LinkMeet.Domain.Enums;
using LinkMeet.Domain.Interfaces;
using LinkMeet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LinkMeet.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public async Task<User?> GetByIdAsync(Guid id) =>
        await _db.Users.FindAsync(id);

    public async Task<User?> GetByEmailAsync(string email) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower());

    public async Task<User> CreateAsync(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<bool> EmailExistsAsync(string email) =>
        await _db.Users.AnyAsync(u => u.Email == email.ToLower());
}

public class MeetingRepository : IMeetingRepository
{
    private readonly AppDbContext _db;
    public MeetingRepository(AppDbContext db) => _db = db;

    public async Task<Meeting?> GetByIdAsync(Guid id) =>
        await _db.Meetings.FindAsync(id);

    public async Task<Meeting?> GetByCodeAsync(string meetingCode) =>
        await _db.Meetings.FirstOrDefaultAsync(m => m.MeetingCode == meetingCode);

    public async Task<List<Meeting>> GetByHostIdAsync(Guid hostId) =>
        await _db.Meetings.Where(m => m.HostId == hostId).OrderByDescending(m => m.CreatedAt).ToListAsync();

    public async Task<List<Meeting>> GetUpcomingByUserIdAsync(Guid userId)
    {
        var meetingIds = await _db.Participants
            .Where(p => p.UserId == userId)
            .Select(p => p.MeetingId)
            .ToListAsync();

        return await _db.Meetings
            .Where(m => meetingIds.Contains(m.Id) &&
                        (m.Status == MeetingStatus.Scheduled || m.Status == MeetingStatus.Active))
            .OrderBy(m => m.ScheduledAt ?? m.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Meeting>> GetPastByUserIdAsync(Guid userId)
    {
        var meetingIds = await _db.Participants
            .Where(p => p.UserId == userId)
            .Select(p => p.MeetingId)
            .ToListAsync();

        return await _db.Meetings
            .Where(m => meetingIds.Contains(m.Id) &&
                        (m.Status == MeetingStatus.Ended || m.Status == MeetingStatus.Cancelled))
            .OrderByDescending(m => m.EndedAt ?? m.CreatedAt)
            .ToListAsync();
    }

    public async Task<Meeting> CreateAsync(Meeting meeting)
    {
        _db.Meetings.Add(meeting);
        await _db.SaveChangesAsync();
        return meeting;
    }

    public async Task UpdateAsync(Meeting meeting)
    {
        _db.Meetings.Update(meeting);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var meeting = await _db.Meetings.FindAsync(id);
        if (meeting != null)
        {
            _db.Meetings.Remove(meeting);
            await _db.SaveChangesAsync();
        }
    }
}

public class ParticipantRepository : IParticipantRepository
{
    private readonly AppDbContext _db;
    public ParticipantRepository(AppDbContext db) => _db = db;

    public async Task<Participant?> GetAsync(Guid meetingId, Guid userId) =>
        await _db.Participants.FirstOrDefaultAsync(p => p.MeetingId == meetingId && p.UserId == userId);

    public async Task<List<Participant>> GetByMeetingIdAsync(Guid meetingId)
    {
        var participants = await _db.Participants.Where(p => p.MeetingId == meetingId && p.LeftAt == null).ToListAsync();
        var userIds = participants.Select(p => p.UserId).Distinct().ToList();
        var users = await _db.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id);
        foreach (var p in participants)
        {
            if (users.TryGetValue(p.UserId, out var user))
            {
                p.User = user;
            }
        }
        return participants;
    }

    public async Task<Participant> CreateAsync(Participant participant)
    {
        _db.Participants.Add(participant);
        await _db.SaveChangesAsync();
        return participant;
    }

    public async Task UpdateAsync(Participant participant)
    {
        _db.Participants.Update(participant);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var p = await _db.Participants.FindAsync(id);
        if (p != null)
        {
            _db.Participants.Remove(p);
            await _db.SaveChangesAsync();
        }
    }
}

public class ChatMessageRepository : IChatMessageRepository
{
    private readonly AppDbContext _db;
    public ChatMessageRepository(AppDbContext db) => _db = db;

    public async Task<List<ChatMessage>> GetByMeetingIdAsync(Guid meetingId)
    {
        var messages = await _db.ChatMessages
            .Where(c => c.MeetingId == meetingId)
            .OrderBy(c => c.SentAt)
            .ToListAsync();

        var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();
        var senders = await _db.Users.Where(u => senderIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id);

        foreach (var message in messages)
        {
            if (senders.TryGetValue(message.SenderId, out var sender))
            {
                message.Sender = sender;
            }
        }

        return messages;
    }

    public async Task<ChatMessage> CreateAsync(ChatMessage message)
    {
        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();
        return message;
    }
}
