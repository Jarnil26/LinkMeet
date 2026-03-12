using System.Security.Claims;
using LinkMeet.Application.DTOs;
using LinkMeet.Domain.Entities;
using LinkMeet.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LinkMeet.Infrastructure.Hubs;

[Authorize]
public class MeetingHub : Hub
{
    private readonly IChatMessageRepository _chatRepo;
    private readonly IParticipantRepository _participantRepo;
    private readonly IUserRepository _userRepo;

    // Track connected users: ConnectionId -> (UserId, MeetingId, DisplayName)
    private static readonly Dictionary<string, (Guid UserId, Guid MeetingId, string DisplayName)> ConnectedUsers = new();

    public MeetingHub(
        IChatMessageRepository chatRepo,
        IParticipantRepository participantRepo,
        IUserRepository userRepo)
    {
        _chatRepo = chatRepo;
        _participantRepo = participantRepo;
        _userRepo = userRepo;
    }

    public async Task JoinMeeting(string meetingId)
    {
        var userId = GetUserId();
        var meetingGuid = Guid.Parse(meetingId);
        var user = await _userRepo.GetByIdAsync(userId);
        var displayName = user?.DisplayName ?? "Unknown";

        await Groups.AddToGroupAsync(Context.ConnectionId, meetingId);
        ConnectedUsers[Context.ConnectionId] = (userId, meetingGuid, displayName);

        // Send list of EXISTING participants to the new joiner ONLY
        var existingUsers = ConnectedUsers
            .Where(kv => kv.Value.MeetingId == meetingGuid && kv.Key != Context.ConnectionId)
            .Select(kv => new
            {
                UserId = kv.Value.UserId,
                DisplayName = kv.Value.DisplayName,
                ConnectionId = kv.Key
            })
            .ToList();

        await Clients.Caller.SendAsync("ExistingParticipants", existingUsers);

        // Notify OTHERS (not the joiner) that someone new joined
        await Clients.OthersInGroup(meetingId).SendAsync("UserJoined", new
        {
            UserId = userId,
            DisplayName = displayName,
            ConnectionId = Context.ConnectionId
        });
    }

    public async Task LeaveMeeting(string meetingId)
    {
        var userId = GetUserId();

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, meetingId);
        ConnectedUsers.Remove(Context.ConnectionId);

        // Mark participant as left
        var meetingGuid = Guid.Parse(meetingId);
        var participant = await _participantRepo.GetAsync(meetingGuid, userId);
        if (participant != null)
        {
            participant.LeftAt = DateTime.UtcNow;
            await _participantRepo.UpdateAsync(participant);
        }

        var user = await _userRepo.GetByIdAsync(userId);
        await Clients.OthersInGroup(meetingId).SendAsync("UserLeft", new
        {
            UserId = userId,
            DisplayName = user?.DisplayName ?? "Unknown"
        });
    }

    public async Task SendMessage(string meetingId, string content)
    {
        var userId = GetUserId();
        var user = await _userRepo.GetByIdAsync(userId);

        var message = new ChatMessage
        {
            MeetingId = Guid.Parse(meetingId),
            SenderId = userId,
            Content = content
        };
        await _chatRepo.CreateAsync(message);

        await Clients.Group(meetingId).SendAsync("ReceiveMessage", new ChatMessageDto
        {
            Id = message.Id,
            SenderId = userId,
            SenderName = user?.DisplayName ?? "Unknown",
            Content = content,
            SentAt = message.SentAt
        });
    }

    // WebRTC Signaling
    public async Task SendSignal(string meetingId, string targetConnectionId, string signalType, string signalData)
    {
        await Clients.Client(targetConnectionId).SendAsync("ReceiveSignal", new
        {
            SenderConnectionId = Context.ConnectionId,
            SignalType = signalType,
            SignalData = signalData
        });
    }

    public async Task ToggleAudio(string meetingId, bool isOn)
    {
        var userId = GetUserId();
        await Clients.OthersInGroup(meetingId).SendAsync("AudioToggled", new { UserId = userId, IsOn = isOn });
    }

    public async Task ToggleVideo(string meetingId, bool isOn)
    {
        var userId = GetUserId();
        await Clients.OthersInGroup(meetingId).SendAsync("VideoToggled", new { UserId = userId, IsOn = isOn });
    }

    public async Task StartScreenShare(string meetingId)
    {
        var userId = GetUserId();
        await Clients.OthersInGroup(meetingId).SendAsync("ScreenShareStarted", new { UserId = userId, ConnectionId = Context.ConnectionId });
    }

    public async Task StopScreenShare(string meetingId)
    {
        var userId = GetUserId();
        await Clients.OthersInGroup(meetingId).SendAsync("ScreenShareStopped", new { UserId = userId });
    }

    // Host controls
    public async Task MuteParticipant(string meetingId, string targetUserId)
    {
        await Clients.Group(meetingId).SendAsync("ParticipantMuted", new { UserId = targetUserId });
    }

    public async Task RemoveParticipant(string meetingId, string targetUserId)
    {
        await Clients.Group(meetingId).SendAsync("ParticipantRemoved", new { UserId = targetUserId });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (ConnectedUsers.TryGetValue(Context.ConnectionId, out var info))
        {
            ConnectedUsers.Remove(Context.ConnectionId);
            await Clients.Group(info.MeetingId.ToString()).SendAsync("UserLeft", new
            {
                UserId = info.UserId,
                DisplayName = info.DisplayName
            });
        }
        await base.OnDisconnectedAsync(exception);
    }

    private Guid GetUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim != null ? Guid.Parse(claim) : throw new HubException("User not authenticated");
    }
}
