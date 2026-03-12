using System.Security.Claims;
using LinkMeet.Application.DTOs;
using LinkMeet.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkMeet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MeetingsController : ControllerBase
{
    private readonly IMeetingService _meetingService;

    public MeetingsController(IMeetingService meetingService) => _meetingService = meetingService;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMeetingDto dto)
    {
        try
        {
            var userId = GetUserId();
            var result = await _meetingService.CreateAsync(userId, dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _meetingService.GetByIdAsync(id);
        return result != null ? Ok(result) : NotFound();
    }

    [HttpGet("code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        var result = await _meetingService.GetByCodeAsync(code);
        return result != null ? Ok(result) : NotFound();
    }

    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcoming()
    {
        var userId = GetUserId();
        var result = await _meetingService.GetUpcomingAsync(userId);
        return Ok(result);
    }

    [HttpGet("past")]
    public async Task<IActionResult> GetPast()
    {
        var userId = GetUserId();
        var result = await _meetingService.GetPastAsync(userId);
        return Ok(result);
    }

    [HttpPost("join")]
    public async Task<IActionResult> Join([FromBody] JoinMeetingDto dto)
    {
        try
        {
            var userId = GetUserId();
            var result = await _meetingService.JoinAsync(userId, dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            var userId = GetUserId();
            await _meetingService.CancelAsync(id, userId);
            return Ok(new { message = "Meeting cancelled" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/end")]
    public async Task<IActionResult> End(Guid id)
    {
        try
        {
            var userId = GetUserId();
            await _meetingService.EndAsync(id, userId);
            return Ok(new { message = "Meeting ended" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim != null ? Guid.Parse(claim) : throw new UnauthorizedAccessException();
    }
}
