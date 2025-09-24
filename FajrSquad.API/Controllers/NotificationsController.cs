using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FajrSquad.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FirebaseAdmin.Messaging;
using FajrSquad.Infrastructure.Services;
using FajrSquad.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace FajrSquad.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly FajrDbContext _db;
        private readonly INotificationSender _notificationSender;
        private readonly IMessageBuilder _messageBuilder;
        private readonly INotificationScheduler _notificationScheduler;

        public NotificationsController(
            FajrDbContext db, 
            INotificationSender notificationSender,
            IMessageBuilder messageBuilder,
            INotificationScheduler notificationScheduler)
        {
            _db = db;
            _notificationSender = notificationSender;
            _messageBuilder = messageBuilder;
            _notificationScheduler = notificationScheduler;
        }

        // ✅ INVIA NOTIFICA AL FRÈRE MOTIVATEUR
        [Authorize]
        [HttpPost("notify-motivating-brother")]
        public async Task<IActionResult> NotifyMotivatingBrother()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var today = DateTime.UtcNow.Date;

            // Verifica check-in
            var hasCheckedIn = await _db.FajrCheckIns
                .AnyAsync(c => c.UserId == userId && c.Date == today);

            if (hasCheckedIn)
                return BadRequest("Hai già fatto check-in oggi.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || string.IsNullOrEmpty(user.MotivatingBrother))
                return BadRequest("Frère motivateur non configurato.");

            // Trova il fratello motivatore
            var motivatingBrother = await _db.Users
                .FirstOrDefaultAsync(u =>
                    u.Phone == user.MotivatingBrother || u.Name == user.MotivatingBrother);

            if (motivatingBrother == null)
                return NotFound("Frère motivateur non trovato.");

            // Trova il suo token FCM
            var token = await _db.DeviceTokens
                .Where(t => t.UserId == motivatingBrother.Id && t.IsActive)
                .Select(t => t.Token)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(token))
                return BadRequest("Token FCM non trovato per il frère motivateur.");

            // Invia notifica
            var message = new Message
            {
                Notification = new Notification
                {
                    Title = "Il tuo fratello ha bisogno di motivazione",
                    Body = $"{user.Name} non ha fatto check-in oggi. Incoraggialo!"
                },
                Token = token,
                Data = new Dictionary<string, string>
                {
                    ["action"] = "open_app",
                    ["screen"] = "motivating_brother",
                    ["userId"] = user.Id.ToString()
                }
            };

            try
            {
                var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                return Ok(new { message = "Notifica inviata con successo", messageId = response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Errore nell'invio della notifica", details = ex.Message });
            }
        }

        // ✅ REGISTRA DEVICE TOKEN
        [Authorize]
        [HttpPost("devices/register")]
        public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            try
            {
                // Check if device token already exists
                var existingToken = await _db.DeviceTokens
                    .FirstOrDefaultAsync(dt => dt.UserId == userId && dt.Token == request.Token);

                if (existingToken != null)
                {
                    // Update existing token
                    existingToken.Platform = request.Platform;
                    existingToken.Language = request.Language;
                    existingToken.TimeZone = request.TimeZone;
                    existingToken.AppVersion = request.AppVersion;
                    existingToken.IsActive = true;
                    existingToken.UpdatedAt = DateTimeOffset.UtcNow;
                }
                else
                {
                    // Create new device token
                    var deviceToken = new DeviceToken
                    {
                        UserId = userId,
                        Token = request.Token,
                        Platform = request.Platform,
                        Language = request.Language,
                        TimeZone = request.TimeZone,
                        AppVersion = request.AppVersion,
                        IsActive = true
                    };

                    _db.DeviceTokens.Add(deviceToken);
                }

                await _db.SaveChangesAsync();

                return Ok(new { message = "Device token registrato con successo" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Errore nella registrazione del device token", details = ex.Message });
            }
        }

        // ✅ AGGIORNA PREFERENZE NOTIFICHE
        [Authorize]
        [HttpPut("preferences")]
        public async Task<IActionResult> UpdatePreferences([FromBody] UpdateNotificationPreferencesRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            try
            {
                var preferences = await _db.UserNotificationPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (preferences == null)
                {
                    preferences = new UserNotificationPreference
                    {
                        UserId = userId
                    };
                    _db.UserNotificationPreferences.Add(preferences);
                }

                preferences.Morning = request.Morning;
                preferences.Evening = request.Evening;
                preferences.FajrMissed = request.FajrMissed;
                preferences.Escalation = request.Escalation;
                preferences.HadithDaily = request.HadithDaily;
                preferences.MotivationDaily = request.MotivationDaily;
                preferences.EventsNew = request.EventsNew;
                preferences.EventsReminder = request.EventsReminder;
                preferences.QuietHoursStart = request.QuietHoursStart;
                preferences.QuietHoursEnd = request.QuietHoursEnd;

                await _db.SaveChangesAsync();

                return Ok(new { message = "Preferenze notifiche aggiornate con successo" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Errore nell'aggiornamento delle preferenze", details = ex.Message });
            }
        }

        // ✅ OTTIENI PREFERENZE NOTIFICHE
        [Authorize]
        [HttpGet("preferences")]
        public async Task<IActionResult> GetPreferences()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            try
            {
                var preferences = await _db.UserNotificationPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (preferences == null)
                {
                    // Return default preferences
                    preferences = new UserNotificationPreference
                    {
                        UserId = userId
                    };
                }

                return Ok(preferences);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Errore nel recupero delle preferenze", details = ex.Message });
            }
        }

        // ✅ DEBUG: INVIA NOTIFICA DI TEST (SOLO ADMIN)
        [Authorize]
        [HttpPost("debug/send")]
        public async Task<IActionResult> SendTestNotification([FromBody] SendTestNotificationRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            // TODO: Add admin check here
            // For now, allow any authenticated user for testing

            try
            {
                var notificationRequest = new NotificationRequest
                {
                    Title = request.Title,
                    Body = request.Body,
                    Data = request.Data ?? new Dictionary<string, string>(),
                    Priority = request.Priority
                };

                NotificationResult result;
                if (request.UserId.HasValue)
                {
                    result = await _notificationSender.SendToUserAsync(request.UserId.Value, notificationRequest);
                }
                else
                {
                    result = await _notificationSender.SendToUserAsync(userId, notificationRequest);
                }

                if (result.Success)
                {
                    return Ok(new { message = "Notifica di test inviata con successo", result });
                }
                else
                {
                    return BadRequest(new { error = "Errore nell'invio della notifica di test", result });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Errore nell'invio della notifica di test", details = ex.Message });
            }
        }

        // ✅ OTTIENI LOG NOTIFICHE
        [Authorize]
        [HttpGet("logs")]
        public async Task<IActionResult> GetNotificationLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            try
            {
                var logs = await _db.NotificationLogs
                    .Where(nl => nl.UserId == userId)
                    .OrderByDescending(nl => nl.SentAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(nl => new
                    {
                        nl.Id,
                        nl.Type,
                        nl.Result,
                        nl.SentAt,
                        nl.Error
                    })
                    .ToListAsync();

                return Ok(logs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Errore nel recupero dei log", details = ex.Message });
            }
        }
    }

    // DTOs
    public class RegisterDeviceRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;
        
        [Required]
        public string Platform { get; set; } = "Android";
        
        [Required]
        public string Language { get; set; } = "it";
        
        [Required]
        public string TimeZone { get; set; } = "Africa/Dakar";
        
        public string? AppVersion { get; set; }
    }

    public class UpdateNotificationPreferencesRequest
    {
        public bool Morning { get; set; } = true;
        public bool Evening { get; set; } = true;
        public bool FajrMissed { get; set; } = true;
        public bool Escalation { get; set; } = true;
        public bool HadithDaily { get; set; } = true;
        public bool MotivationDaily { get; set; } = true;
        public bool EventsNew { get; set; } = true;
        public bool EventsReminder { get; set; } = true;
        public TimeSpan? QuietHoursStart { get; set; }
        public TimeSpan? QuietHoursEnd { get; set; }
    }

    public class SendTestNotificationRequest
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Body { get; set; } = string.Empty;
        
        public Dictionary<string, string>? Data { get; set; }
        public Guid? UserId { get; set; }
        public FajrSquad.Infrastructure.Services.NotificationPriority Priority { get; set; } = FajrSquad.Infrastructure.Services.NotificationPriority.Normal;
    }
}