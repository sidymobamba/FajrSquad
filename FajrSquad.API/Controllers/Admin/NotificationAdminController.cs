using FajrSquad.Core.Entities;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace FajrSquad.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/notifications")]
    [Authorize(Roles = "Admin")]
    public class NotificationAdminController : ControllerBase
    {
        private readonly FajrDbContext _context;
        private readonly INotificationMetricsService _metricsService;
        private readonly ILogger<NotificationAdminController> _logger;

        public NotificationAdminController(
            FajrDbContext context,
            INotificationMetricsService metricsService,
            ILogger<NotificationAdminController> logger)
        {
            _context = context;
            _metricsService = metricsService;
            _logger = logger;
        }

        /// <summary>
        /// Get notification system health and metrics
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> GetHealth()
        {
            try
            {
                var health = await _metricsService.GetSystemHealthAsync();
                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification system health");
                return StatusCode(500, new { error = "Failed to get system health" });
            }
        }

        /// <summary>
        /// Get notification statistics and metrics
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            try
            {
                var fromDate = from ?? DateTime.UtcNow.AddDays(-7);
                var toDate = to ?? DateTime.UtcNow;

                var stats = await _metricsService.GetNotificationStatsAsync(fromDate, toDate);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification stats");
                return StatusCode(500, new { error = "Failed to get notification stats" });
            }
        }

        /// <summary>
        /// Get pending notifications in queue
        /// </summary>
        [HttpGet("queue/pending")]
        public async Task<IActionResult> GetPendingNotifications([FromQuery] int limit = 100)
        {
            try
            {
                var pending = await _context.ScheduledNotifications
                    .Where(sn => sn.Status == "Pending" && sn.ExecuteAt <= DateTimeOffset.UtcNow)
                    .OrderBy(sn => sn.ExecuteAt)
                    .Take(limit)
                    .Select(sn => new
                    {
                        sn.Id,
                        sn.UserId,
                        sn.Type,
                        sn.ExecuteAt,
                        sn.UniqueKey,
                        sn.Retries,
                        sn.MaxRetries
                    })
                    .ToListAsync();

                return Ok(new { count = pending.Count, notifications = pending });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending notifications");
                return StatusCode(500, new { error = "Failed to get pending notifications" });
            }
        }

        /// <summary>
        /// Get failed notifications
        /// </summary>
        [HttpGet("queue/failed")]
        public async Task<IActionResult> GetFailedNotifications([FromQuery] int limit = 100)
        {
            try
            {
                var failed = await _context.ScheduledNotifications
                    .Where(sn => sn.Status == "Failed")
                    .OrderByDescending(sn => sn.ProcessedAt)
                    .Take(limit)
                    .Select(sn => new
                    {
                        sn.Id,
                        sn.UserId,
                        sn.Type,
                        sn.ExecuteAt,
                        sn.ProcessedAt,
                        sn.ErrorMessage,
                        sn.Retries,
                        sn.MaxRetries,
                        sn.NextRetryAt
                    })
                    .ToListAsync();

                return Ok(new { count = failed.Count, notifications = failed });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting failed notifications");
                return StatusCode(500, new { error = "Failed to get failed notifications" });
            }
        }

        /// <summary>
        /// Retry failed notification
        /// </summary>
        [HttpPost("queue/retry/{id}")]
        public async Task<IActionResult> RetryNotification(int id)
        {
            try
            {
                var notification = await _context.ScheduledNotifications.FindAsync(id);
                if (notification == null)
                {
                    return NotFound(new { error = "Notification not found" });
                }

                if (notification.Status != "Failed")
                {
                    return BadRequest(new { error = "Only failed notifications can be retried" });
                }

                notification.Status = "Pending";
                notification.Retries = 0;
                notification.ErrorMessage = null;
                notification.NextRetryAt = null;
                notification.ExecuteAt = DateTimeOffset.UtcNow.AddMinutes(1);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Retried notification {Id} of type {Type}", id, notification.Type);

                return Ok(new { message = "Notification queued for retry", id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying notification {Id}", id);
                return StatusCode(500, new { error = "Failed to retry notification" });
            }
        }

        /// <summary>
        /// Cancel scheduled notification
        /// </summary>
        [HttpDelete("queue/{id}")]
        public async Task<IActionResult> CancelNotification(int id)
        {
            try
            {
                var notification = await _context.ScheduledNotifications.FindAsync(id);
                if (notification == null)
                {
                    return NotFound(new { error = "Notification not found" });
                }

                if (notification.Status == "Succeeded")
                {
                    return BadRequest(new { error = "Cannot cancel already processed notification" });
                }

                notification.Status = "Cancelled";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cancelled notification {Id} of type {Type}", id, notification.Type);

                return Ok(new { message = "Notification cancelled", id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling notification {Id}", id);
                return StatusCode(500, new { error = "Failed to cancel notification" });
            }
        }

        /// <summary>
        /// Get user notification preferences
        /// </summary>
        [HttpGet("users/{userId}/preferences")]
        public async Task<IActionResult> GetUserPreferences(Guid userId)
        {
            try
            {
                var preferences = await _context.UserNotificationPreferences
                    .Where(p => p.UserId == userId)
                    .Select(p => new
                    {
                        p.UserId,
                        p.Morning,
                        p.Evening,
                        p.FajrMissed,
                        p.Escalation,
                        p.HadithDaily,
                        p.MotivationDaily,
                        p.EventsNew,
                        p.EventsReminder,
                        p.QuietHoursStart,
                        p.QuietHoursEnd
                    })
                    .FirstOrDefaultAsync();

                if (preferences == null)
                {
                    return NotFound(new { error = "User preferences not found" });
                }

                return Ok(preferences);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user preferences for {UserId}", userId);
                return StatusCode(500, new { error = "Failed to get user preferences" });
            }
        }

        /// <summary>
        /// Update user notification preferences
        /// </summary>
        [HttpPut("users/{userId}/preferences")]
        public async Task<IActionResult> UpdateUserPreferences(Guid userId, [FromBody] UpdatePreferencesRequest request)
        {
            try
            {
                var preferences = await _context.UserNotificationPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (preferences == null)
                {
                    return NotFound(new { error = "User preferences not found" });
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
                preferences.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated notification preferences for user {UserId}", userId);

                return Ok(new { message = "Preferences updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user preferences for {UserId}", userId);
                return StatusCode(500, new { error = "Failed to update user preferences" });
            }
        }

        /// <summary>
        /// Get user notification logs
        /// </summary>
        [HttpGet("users/{userId}/logs")]
        public async Task<IActionResult> GetUserLogs(Guid userId, [FromQuery] int limit = 50)
        {
            try
            {
                var logs = await _context.NotificationLogs
                    .Where(nl => nl.UserId == userId)
                    .OrderByDescending(nl => nl.CreatedAt)
                    .Take(limit)
                    .Select(nl => new
                    {
                        nl.Id,
                        nl.Type,
                        nl.Result,
                        nl.Error,
                        nl.CreatedAt,
                        nl.ProviderMessageId
                    })
                    .ToListAsync();

                return Ok(new { count = logs.Count, logs });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user logs for {UserId}", userId);
                return StatusCode(500, new { error = "Failed to get user logs" });
            }
        }

        /// <summary>
        /// Get device tokens for user
        /// </summary>
        [HttpGet("users/{userId}/devices")]
        public async Task<IActionResult> GetUserDevices(Guid userId)
        {
            try
            {
                var devices = await _context.DeviceTokens
                    .Where(dt => dt.UserId == userId && !dt.IsDeleted)
                    .Select(dt => new
                    {
                        dt.Id,
                        dt.Platform,
                        dt.Language,
                        dt.TimeZone,
                        dt.IsActive,
                        dt.CreatedAt,
                        dt.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(new { count = devices.Count, devices });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user devices for {UserId}", userId);
                return StatusCode(500, new { error = "Failed to get user devices" });
            }
        }

        /// <summary>
        /// Clean up old notification logs
        /// </summary>
        [HttpPost("cleanup/logs")]
        public async Task<IActionResult> CleanupLogs([FromQuery] int daysToKeep = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                
                var deletedCount = await _context.NotificationLogs
                    .Where(nl => nl.CreatedAt < cutoffDate)
                    .ExecuteDeleteAsync();

                _logger.LogInformation("Cleaned up {Count} old notification logs", deletedCount);

                return Ok(new { message = $"Cleaned up {deletedCount} old logs", deletedCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up notification logs");
                return StatusCode(500, new { error = "Failed to cleanup logs" });
            }
        }

        /// <summary>
        /// Get system configuration
        /// </summary>
        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            try
            {
                var config = new
                {
                    UseFakeSender = Environment.GetEnvironmentVariable("Notifications__UseFakeSender") ?? "true",
                    ForceWindow = Environment.GetEnvironmentVariable("Notifications__ForceWindow") ?? "false",
                    MorningTime = Environment.GetEnvironmentVariable("Notifications__MorningTime") ?? "06:30",
                    EveningTime = Environment.GetEnvironmentVariable("Notifications__EveningTime") ?? "21:30",
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
                };

                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system configuration");
                return StatusCode(500, new { error = "Failed to get configuration" });
            }
        }
    }

    public class UpdatePreferencesRequest
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
}
