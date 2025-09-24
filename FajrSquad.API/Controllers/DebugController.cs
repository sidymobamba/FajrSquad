using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Infrastructure.Services;
using FajrSquad.Core.Entities;
using System.Security.Claims;
using TimeZoneConverter;
using System.Text.Json;

namespace FajrSquad.API.Controllers
{
    [ApiController]
    [Route("debug")]
    [Authorize]
    public class DebugController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly FajrDbContext _context;
        private readonly ILogger<DebugController> _logger;
        private readonly INotificationScheduler _scheduler;
        private readonly INotificationSender _notificationSender;

        public DebugController(
            IWebHostEnvironment env,
            FajrDbContext context,
            ILogger<DebugController> logger,
            INotificationScheduler scheduler,
            INotificationSender notificationSender)
        {
            _env = env;
            _context = context;
            _logger = logger;
            _scheduler = scheduler;
            _notificationSender = notificationSender;
        }

        private IActionResult NotDev() => Problem("Debug endpoints available only in Development.");
        private bool IsDev => _env.IsDevelopment();

        [HttpPost("seed-user")]
        public async Task<IActionResult> SeedUser([FromBody] SeedUserRequest? request = null)
        {
            if (!IsDev) return NotDev();

            try
            {
                var testPhone = "+2210000000";
                var testEmail = "test@fajrsquad.local";
                var timezone = NormalizeTz(request?.TimeZone);
                var token = request?.Token ?? "FAKE_TOKEN_FOR_TESTING";

                // Find or create test user
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Phone == testPhone || u.Email == testEmail);

                if (user == null)
                {
                    user = new User
                    {
                        Name = "Test User",
                        Phone = testPhone,
                        Email = testEmail,
                        City = "Dakar",
                        Country = "Senegal",
                        Role = "User",
                        FajrStreak = 0,
                        RegisteredAt = DateTime.UtcNow
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created test user {UserId}", user.Id);
                }

                // Create/update notification preferences (all ON)
                var preferences = await _context.UserNotificationPreferences
                    .FirstOrDefaultAsync(p => p.UserId == user.Id);

                if (preferences == null)
                {
                    preferences = new UserNotificationPreference
                    {
                        UserId = user.Id,
                        Morning = true,
                        Evening = true,
                        FajrMissed = true,
                        Escalation = true,
                        HadithDaily = true,
                        MotivationDaily = true,
                        EventsNew = true,
                        EventsReminder = true
                    };
                    _context.UserNotificationPreferences.Add(preferences);
                }
                else
                {
                    preferences.Morning = true;
                    preferences.Evening = true;
                    preferences.FajrMissed = true;
                    preferences.Escalation = true;
                    preferences.HadithDaily = true;
                    preferences.MotivationDaily = true;
                    preferences.EventsNew = true;
                    preferences.EventsReminder = true;
                }

                // Create/update device token
                var deviceToken = await _context.DeviceTokens
                    .FirstOrDefaultAsync(dt => dt.UserId == user.Id && dt.Token == token);

                if (deviceToken == null)
                {
                    deviceToken = new DeviceToken
                    {
                        UserId = user.Id,
                        Token = token,
                        Platform = "Android",
                        Language = "it",
                        TimeZone = timezone,
                        AppVersion = "1.0.0",
                        IsActive = true
                    };
                    _context.DeviceTokens.Add(deviceToken);
                }
                else
                {
                    deviceToken.TimeZone = timezone;
                    deviceToken.IsActive = true;
                    deviceToken.UpdatedAt = DateTimeOffset.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Seeded test user {UserId} with timezone {Timezone}", user.Id, timezone);

                return Ok(new
                {
                    userId = user.Id,
                    token = deviceToken.Token,
                    timezone = deviceToken.TimeZone,
                    preferences = new
                    {
                        morning = preferences.Morning,
                        evening = preferences.Evening,
                        fajrMissed = preferences.FajrMissed,
                        escalation = preferences.Escalation,
                        hadithDaily = preferences.HadithDaily,
                        motivationDaily = preferences.MotivationDaily,
                        eventsNew = preferences.EventsNew,
                        eventsReminder = preferences.EventsReminder
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding test user");
                return StatusCode(500, new { error = "Error seeding test user", details = ex.Message });
            }
        }

        [HttpPost("enqueue")]
        public async Task<IActionResult> Enqueue([FromBody] EnqueueRequest request)
        {
            if (!IsDev) return NotDev();

            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var executeAt = DateTimeOffset.UtcNow.AddSeconds(request.DelaySeconds);
                var uniqueKey = $"{request.Type}:{userId}:{executeAt:yyyyMMddHHmmss}";

                var scheduledNotification = new ScheduledNotification
                {
                    UserId = userId,
                    Type = request.Type,
                    ExecuteAt = executeAt,
                    DataJson = JsonSerializer.Serialize(new { UserId = userId, Debug = true }),
                    Status = "Pending",
                    UniqueKey = uniqueKey
                };

                _context.ScheduledNotifications.Add(scheduledNotification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Enqueued {Type} notification for user {UserId} at {ExecuteAt}", 
                    request.Type, userId, executeAt);

                return Ok(new
                {
                    scheduledNotificationId = scheduledNotification.Id,
                    userId = userId,
                    type = request.Type,
                    executeAt = executeAt,
                    uniqueKey = uniqueKey
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enqueueing notification");
                return StatusCode(500, new { error = "Error enqueueing notification", details = ex.Message });
            }
        }

        [HttpPost("push")]
        public async Task<IActionResult> Push()
        {
            if (!IsDev) return NotDev();

            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                
                // Send to current user
                var notificationRequest = new NotificationRequest
                {
                    Title = "Test Push",
                    Body = "Hello from backend debug endpoint",
                    Data = new Dictionary<string, string> { { "debug", "true" } }
                };

                var result = await _notificationSender.SendToUserAsync(userId, notificationRequest);
                
                _logger.LogInformation("Sent test push notification to user {UserId}, messageId: {MessageId}", 
                    userId, result.MessageId);

                return Ok(new { messageId = result.MessageId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending push notification");
                return StatusCode(500, new { error = "Error sending push notification", details = ex.Message });
            }
        }

        [HttpGet("when")]
        public async Task<IActionResult> When()
        {
            if (!IsDev) return NotDev();

            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                
                var user = await _context.Users
                    .Include(u => u.DeviceTokens.Where(dt => dt.IsActive))
                    .Include(u => u.UserNotificationPreferences)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                var deviceToken = user.DeviceTokens?.FirstOrDefault();
                var preferences = user.UserNotificationPreferences?.FirstOrDefault();

                var timezone = NormalizeTz(deviceToken?.TimeZone);
                var tz = TZConvert.GetTimeZoneInfo(timezone);
                var nowUtc = DateTimeOffset.UtcNow;
                var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc.DateTime, tz);

                var morningTarget = TimeSpan.Parse("06:30");
                var eveningTarget = TimeSpan.Parse("21:30");
                var force = true; // ForceWindow is always true in Development

                return Ok(new
                {
                    nowUtc = nowUtc,
                    deviceTimeZone = timezone,
                    nowLocal = nowLocal,
                    morningTarget = morningTarget,
                    eveningTarget = eveningTarget,
                    force = force,
                    preferences = preferences != null ? new
                    {
                        morning = preferences.Morning,
                        evening = preferences.Evening,
                        fajrMissed = preferences.FajrMissed,
                        escalation = preferences.Escalation,
                        hadithDaily = preferences.HadithDaily,
                        motivationDaily = preferences.MotivationDaily,
                        eventsNew = preferences.EventsNew,
                        eventsReminder = preferences.EventsReminder
                    } : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting timezone info");
                return StatusCode(500, new { error = "Error getting timezone info", details = ex.Message });
            }
        }

        [HttpGet("pending")]
        public async Task<IActionResult> Pending()
        {
            if (!IsDev) return NotDev();

            try
            {
                var now = DateTimeOffset.UtcNow;
                var pending = await _context.ScheduledNotifications
                    .Where(sn => sn.Status == "Pending" && sn.ExecuteAt <= now)
                    .OrderBy(sn => sn.ExecuteAt)
                    .Select(sn => new
                    {
                        sn.Id,
                        sn.UserId,
                        sn.Type,
                        sn.ExecuteAt,
                        sn.UniqueKey,
                        sn.DataJson,
                        delaySeconds = (int)(now - sn.ExecuteAt).TotalSeconds
                    })
                    .ToListAsync();

                return Ok(new
                {
                    count = pending.Count,
                    pending = pending
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending notifications");
                return StatusCode(500, new { error = "Error getting pending notifications", details = ex.Message });
            }
        }

        [HttpGet("logs")]
        public async Task<IActionResult> Logs([FromQuery] int last = 50)
        {
            if (!IsDev) return NotDev();

            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                
                var query = _context.NotificationLogs
                    .Where(nl => nl.UserId == userId);

                var logs = await query
                    .OrderByDescending(nl => nl.SentAt)
                    .Take(last)
                    .Select(nl => new
                    {
                        nl.Id,
                        nl.UserId,
                        nl.Type,
                        nl.Result,
                        nl.ProviderMessageId,
                        nl.Error,
                        nl.SentAt,
                        nl.Retried
                    })
                    .ToListAsync();

                return Ok(new
                {
                    count = logs.Count,
                    logs = logs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification logs");
                return StatusCode(500, new { error = "Error getting notification logs", details = ex.Message });
            }
        }

        private string NormalizeTz(string? tz)
        {
            if (string.IsNullOrWhiteSpace(tz)) return "Africa/Dakar";
            try 
            { 
                _ = TZConvert.GetTimeZoneInfo(tz); 
                return tz; 
            }
            catch 
            { 
                return "Africa/Dakar"; 
            }
        }
    }

    public class SeedUserRequest
    {
        public string? Token { get; set; }
        public string? TimeZone { get; set; }
    }

    public class EnqueueRequest
    {
        public string Type { get; set; } = "Debug";
        public int DelaySeconds { get; set; } = 60;
    }
}
