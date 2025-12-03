using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Core.DTOs;
using System.Security.Claims;

namespace FajrSquad.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly FajrDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(FajrDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("fix-timezone-data")]
        public async Task<IActionResult> FixTimezoneData()
        {
            try
            {
                // Find all device tokens with invalid timezone values
                var invalidTokens = await _context.DeviceTokens
                    .Where(dt => dt.TimeZone == "string" || string.IsNullOrEmpty(dt.TimeZone) || dt.TimeZone.Length < 3)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} device tokens with invalid timezone data", invalidTokens.Count);

                foreach (var token in invalidTokens)
                {
                    // Set default timezone based on user's country or use Africa/Dakar as fallback
                    var user = await _context.Users.FindAsync(token.UserId);
                    if (user != null)
                    {
                        // Map common countries to timezones
                        token.TimeZone = user.Country?.ToLower() switch
                        {
                            "italy" => "Europe/Rome",
                            "france" => "Europe/Paris",
                            "senegal" => "Africa/Dakar",
                            "morocco" => "Africa/Casablanca",
                            "algeria" => "Africa/Algiers",
                            "tunisia" => "Africa/Tunis",
                            "egypt" => "Africa/Cairo",
                            "turkey" => "Europe/Istanbul",
                            "germany" => "Europe/Berlin",
                            "spain" => "Europe/Madrid",
                            "united kingdom" or "uk" => "Europe/London",
                            "united states" or "usa" => "America/New_York",
                            _ => "Africa/Dakar" // Default fallback
                        };
                        
                        token.UpdatedAt = DateTimeOffset.UtcNow;
                        _logger.LogInformation("Updated timezone for user {UserId} from '{OldTimezone}' to '{NewTimezone}'", 
                            token.UserId, "string", token.TimeZone);
                    }
                }

                await _context.SaveChangesAsync();
                
                return Ok(new { 
                    message = $"Fixed {invalidTokens.Count} device tokens with invalid timezone data",
                    fixedCount = invalidTokens.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing timezone data");
                return StatusCode(500, new { error = "Error fixing timezone data", details = ex.Message });
            }
        }

        [HttpGet("timezone-stats")]
        public async Task<IActionResult> GetTimezoneStats()
        {
            try
            {
                var stats = await _context.DeviceTokens
                    .GroupBy(dt => dt.TimeZone)
                    .Select(g => new { TimeZone = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();

                var invalidCount = await _context.DeviceTokens
                    .CountAsync(dt => dt.TimeZone == "string" || string.IsNullOrEmpty(dt.TimeZone) || dt.TimeZone.Length < 3);

                return Ok(new { 
                    timezoneStats = stats,
                    invalidTimezoneCount = invalidCount,
                    totalDeviceTokens = await _context.DeviceTokens.CountAsync()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting timezone stats");
                return StatusCode(500, new { error = "Error getting timezone stats", details = ex.Message });
            }
        }

        [HttpPost("test-timezone")]
        public async Task<IActionResult> TestTimezone()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var deviceToken = await _context.DeviceTokens
                    .FirstOrDefaultAsync(dt => dt.UserId == userId && dt.IsActive);

                if (deviceToken == null)
                {
                    return NotFound(new { error = "No active device token found for user" });
                }

                var timezone = deviceToken.TimeZone ?? "Africa/Dakar";
                var isValid = !string.IsNullOrEmpty(timezone) && timezone != "string" && timezone.Length >= 3;
                
                TimeZoneInfo? userTimeZone = null;
                string? error = null;
                
                if (isValid)
                {
                    try
                    {
                        userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                    }
                    catch (TimeZoneNotFoundException ex)
                    {
                        error = ex.Message;
                        isValid = false;
                    }
                }

                return Ok(new {
                    userId,
                    timezone,
                    isValid,
                    error,
                    userTimeZone = userTimeZone?.Id,
                    currentUtcTime = DateTimeOffset.UtcNow,
                    localTime = userTimeZone != null ? (DateTime?)TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userTimeZone) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing timezone");
                return StatusCode(500, new { error = "Error testing timezone", details = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("seed-adhkar")]
        public async Task<IActionResult> SeedAdhkar()
        {
            try
            {
                await IslamicDataSeeder.SeedAsync(_context);
                
                var adhkarCount = await _context.Adhkar.CountAsync();
                
                return Ok(ApiResponse<object>.SuccessResponse(
                    new { message = "Adhkar seeded successfully", count = adhkarCount },
                    $"Seeder eseguito con successo. {adhkarCount} adhkar nel database."
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding adhkar");
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Errore durante il seeding: {ex.Message}"));
            }
        }
    }
}
