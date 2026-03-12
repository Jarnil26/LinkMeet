using LinkMeet.Application.DTOs;
using LinkMeet.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkMeet.API.Controllers;

[ApiController]
[Route("api/meetings/{meetingId}/[controller]")]
[Authorize]
public class ParticipantsController : ControllerBase
{
    private readonly IParticipantRepository _participantRepo;

    public ParticipantsController(IParticipantRepository participantRepo) =>
        _participantRepo = participantRepo;

    [HttpGet]
    public async Task<IActionResult> GetByMeeting(Guid meetingId)
    {
        var participants = await _participantRepo.GetByMeetingIdAsync(meetingId);
        var result = participants.Select(p => new ParticipantDto
        {
            Id = p.Id,
            UserId = p.UserId,
            DisplayName = p.User?.DisplayName ?? "Unknown",
            AvatarUrl = p.User?.AvatarUrl,
            IsAudioOn = p.IsAudioOn,
            IsVideoOn = p.IsVideoOn,
            Role = p.Role.ToString(),
            JoinedAt = p.JoinedAt
        });
        return Ok(result);
    }
}
