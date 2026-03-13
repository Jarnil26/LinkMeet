using Microsoft.AspNetCore.Mvc;
using LinkMeet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LinkMeet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public HealthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> CheckHealth()
    {
        var healthStatus = new
        {
            Status = "Healthy",
            Checks = new List<object>()
        };

        // 1. Check MongoDB
        try
        {
            // Try to reach the database by executing a simple command or checking if we can connect
            // EF Core MongoDB doesn't have a direct 'CanConnect' that pings in the same way as relational, 
            // but we can try to access a collection.
            var canConnect = await _context.Database.CanConnectAsync();
            healthStatus.Checks.Add(new { Name = "MongoDB Connection", Status = canConnect ? "Up" : "Down" });
            
            if (!canConnect)
            {
                return StatusCode(503, new { Status = "Unhealthy", Checks = healthStatus.Checks });
            }
        }
        catch (Exception ex)
        {
            healthStatus.Checks.Add(new { Name = "MongoDB Connection", Status = "Error", Message = ex.Message });
            return StatusCode(500, new { Status = "Degraded", Checks = healthStatus.Checks });
        }

        // 2. Check JWT Config
        var jwtKey = _configuration["Jwt:Key"];
        healthStatus.Checks.Add(new { Name = "JWT Configuration", Status = !string.IsNullOrEmpty(jwtKey) ? "Valid" : "Missing" });

        return Ok(healthStatus);
    }
}
