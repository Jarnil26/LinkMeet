using LinkMeet.Application.DTOs;
using LinkMeet.Application.Interfaces;
using LinkMeet.Domain.Entities;
using LinkMeet.Domain.Enums;
using LinkMeet.Domain.Interfaces;

namespace LinkMeet.Application.Services;

public class MeetingService : IMeetingService
{
    private readonly IMeetingRepository _meetingRepo;
    private readonly IParticipantRepository _participantRepo;
    private readonly IUserRepository _userRepo;

    public MeetingService(
        IMeetingRepository meetingRepo,
        IParticipantRepository participantRepo,
        IUserRepository userRepo)
    {
        _meetingRepo = meetingRepo;
        _participantRepo = participantRepo;
        _userRepo = userRepo;
    }

    public async Task<MeetingDto> CreateAsync(Guid hostId, CreateMeetingDto dto)
    {
        var host = await _userRepo.GetByIdAsync(hostId)
            ?? throw new Exception("User not found");

        var meeting = new Meeting
        {
            Title = dto.Title,
            HostId = hostId,
            ScheduledAt = dto.ScheduledAt,
            Password = dto.Password,
            HasWaitingRoom = dto.HasWaitingRoom,
            Status = dto.ScheduledAt.HasValue ? MeetingStatus.Scheduled : MeetingStatus.Active
        };

        await _meetingRepo.CreateAsync(meeting);

        // Add host as participant
        await _participantRepo.CreateAsync(new Participant
        {
            MeetingId = meeting.Id,
            UserId = hostId,
            Role = ParticipantRole.Host,
            IsAudioOn = true,
            IsVideoOn = true
        });

        return MapMeeting(meeting, host.DisplayName, 1);
    }

    public async Task<MeetingDto?> GetByIdAsync(Guid id)
    {
        var meeting = await _meetingRepo.GetByIdAsync(id);
        if (meeting == null) return null;

        var host = await _userRepo.GetByIdAsync(meeting.HostId);
        var participants = await _participantRepo.GetByMeetingIdAsync(meeting.Id);

        return MapMeeting(meeting, host?.DisplayName ?? "Unknown", participants.Count);
    }

    public async Task<MeetingDto?> GetByCodeAsync(string code)
    {
        var meeting = await _meetingRepo.GetByCodeAsync(code);
        if (meeting == null) return null;

        var host = await _userRepo.GetByIdAsync(meeting.HostId);
        var participants = await _participantRepo.GetByMeetingIdAsync(meeting.Id);

        return MapMeeting(meeting, host?.DisplayName ?? "Unknown", participants.Count);
    }

    public async Task<List<MeetingDto>> GetUpcomingAsync(Guid userId)
    {
        var meetings = await _meetingRepo.GetUpcomingByUserIdAsync(userId);
        var result = new List<MeetingDto>();
        foreach (var m in meetings)
        {
            var host = await _userRepo.GetByIdAsync(m.HostId);
            var participants = await _participantRepo.GetByMeetingIdAsync(m.Id);
            result.Add(MapMeeting(m, host?.DisplayName ?? "Unknown", participants.Count));
        }
        return result;
    }

    public async Task<List<MeetingDto>> GetPastAsync(Guid userId)
    {
        var meetings = await _meetingRepo.GetPastByUserIdAsync(userId);
        var result = new List<MeetingDto>();
        foreach (var m in meetings)
        {
            var host = await _userRepo.GetByIdAsync(m.HostId);
            var participants = await _participantRepo.GetByMeetingIdAsync(m.Id);
            result.Add(MapMeeting(m, host?.DisplayName ?? "Unknown", participants.Count));
        }
        return result;
    }

    public async Task<MeetingDto> JoinAsync(Guid userId, JoinMeetingDto dto)
    {
        var meeting = await _meetingRepo.GetByCodeAsync(dto.MeetingCode)
            ?? throw new Exception("Meeting not found");

        if (meeting.Status == MeetingStatus.Ended || meeting.Status == MeetingStatus.Cancelled)
            throw new Exception("Meeting is no longer active");

        if (meeting.IsLocked)
            throw new Exception("Meeting is locked");

        if (!string.IsNullOrEmpty(meeting.Password) && meeting.Password != dto.Password)
            throw new Exception("Invalid meeting password");

        // Check if already a participant
        var existing = await _participantRepo.GetAsync(meeting.Id, userId);
        if (existing == null)
        {
            await _participantRepo.CreateAsync(new Participant
            {
                MeetingId = meeting.Id,
                UserId = userId,
                Role = ParticipantRole.Participant
            });
        }

        // Mark meeting active if scheduled
        if (meeting.Status == MeetingStatus.Scheduled)
        {
            meeting.Status = MeetingStatus.Active;
            await _meetingRepo.UpdateAsync(meeting);
        }

        var host = await _userRepo.GetByIdAsync(meeting.HostId);
        var participants = await _participantRepo.GetByMeetingIdAsync(meeting.Id);

        return MapMeeting(meeting, host?.DisplayName ?? "Unknown", participants.Count);
    }

    public async Task CancelAsync(Guid meetingId, Guid userId)
    {
        var meeting = await _meetingRepo.GetByIdAsync(meetingId)
            ?? throw new Exception("Meeting not found");

        if (meeting.HostId != userId)
            throw new Exception("Only the host can cancel the meeting");

        meeting.Status = MeetingStatus.Cancelled;
        await _meetingRepo.UpdateAsync(meeting);
    }

    public async Task EndAsync(Guid meetingId, Guid userId)
    {
        var meeting = await _meetingRepo.GetByIdAsync(meetingId)
            ?? throw new Exception("Meeting not found");

        if (meeting.HostId != userId)
            throw new Exception("Only the host can end the meeting");

        meeting.Status = MeetingStatus.Ended;
        meeting.EndedAt = DateTime.UtcNow;
        await _meetingRepo.UpdateAsync(meeting);
    }

    private static MeetingDto MapMeeting(Meeting m, string hostName, int participantCount) => new()
    {
        Id = m.Id,
        Title = m.Title,
        MeetingCode = m.MeetingCode,
        HostId = m.HostId,
        HostName = hostName,
        ScheduledAt = m.ScheduledAt,
        CreatedAt = m.CreatedAt,
        Status = m.Status.ToString(),
        HasWaitingRoom = m.HasWaitingRoom,
        IsPasswordProtected = !string.IsNullOrEmpty(m.Password),
        ParticipantCount = participantCount
    };
}
