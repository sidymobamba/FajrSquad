using FajrSquad.Infrastructure.Data;
using FajrSquad.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FajrSquad.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/metrics")]
    [Authorize(Roles = "Admin")]
    public class MetricsController : ControllerBase
    {
        private readonly FajrDbContext _context;
        private readonly INotificationMetricsService _metricsService;
        private readonly ILogger<MetricsController> _logger;

        public MetricsController(
            FajrDbContext context,
            INotificationMetricsService metricsService,
            ILogger<MetricsController> logger)
        {
            _context = context;
            _metricsService = metricsService;
            _logger = logger;
        }

        /// <summary>
        /// Get notification delivery metrics
        /// </summary>
        [HttpGet("delivery")]
        public async Task<IActionResult> GetDeliveryMetrics([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            try
            {
                var fromDate = from ?? DateTime.UtcNow.AddDays(-7);
                var toDate = to ?? DateTime.UtcNow;

                var metrics = await _context.NotificationLogs
                    .Where(nl => nl.CreatedAt >= fromDate && nl.CreatedAt <= toDate)
                    .GroupBy(nl => nl.Type)
                    .Select(g => new
                    {
                        Type = g.Key,
                        Total = g.Count(),
                        Successful = g.Count(nl => nl.Result == "Sent"),
                        Failed = g.Count(nl => nl.Result != "Sent"),
                        SuccessRate = g.Count(nl => nl.Result == "Sent") * 100.0 / g.Count()
                    })
                    .ToListAsync();

                return Ok(new { period = new { from = fromDate, to = toDate }, metrics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting delivery metrics");
                return StatusCode(500, new { error = "Failed to get delivery metrics" });
            }
        }

        /// <summary>
        /// Get user engagement metrics
        /// </summary>
        [HttpGet("engagement")]
        public async Task<IActionResult> GetEngagementMetrics([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            try
            {
                var fromDate = from ?? DateTime.UtcNow.AddDays(-7);
                var toDate = to ?? DateTime.UtcNow;

                var totalUsers = await _context.Users.CountAsync(u => !u.IsDeleted);
                var activeUsers = await _context.Users
                    .Where(u => !u.IsDeleted && u.DeviceTokens.Any(dt => dt.IsActive && !dt.IsDeleted))
                    .CountAsync();

                var usersWithPreferences = await _context.UserNotificationPreferences.CountAsync();

                var engagement = new
                {
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    UsersWithPreferences = usersWithPreferences,
                    ActiveUserRate = activeUsers * 100.0 / totalUsers,
                    PreferenceRate = usersWithPreferences * 100.0 / totalUsers
                };

                return Ok(engagement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting engagement metrics");
                return StatusCode(500, new { error = "Failed to get engagement metrics" });
            }
        }

        /// <summary>
        /// Get queue performance metrics
        /// </summary>
        [HttpGet("queue")]
        public async Task<IActionResult> GetQueueMetrics()
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                
                var queueStats = await _context.ScheduledNotifications
                    .GroupBy(sn => sn.Status)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count(),
                        AvgRetries = g.Average(sn => sn.Retries),
                        MaxRetries = g.Max(sn => sn.Retries)
                    })
                    .ToListAsync();

                var overdueCount = await _context.ScheduledNotifications
                    .CountAsync(sn => sn.Status == "Pending" && sn.ExecuteAt < now);

                var metrics = new
                    {
                        QueueStats = queueStats,
                        OverdueCount = overdueCount,
                        Timestamp = now
                    };

                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue metrics");
                return StatusCode(500, new { error = "Failed to get queue metrics" });
            }
        }

        /// <summary>
        /// Get timezone distribution
        /// </summary>
        [HttpGet("timezones")]
        public async Task<IActionResult> GetTimezoneDistribution()
        {
            try
            {
                var timezoneStats = await _context.DeviceTokens
                    .Where(dt => dt.IsActive && !dt.IsDeleted)
                    .GroupBy(dt => dt.TimeZone)
                    .Select(g => new
                    {
                        TimeZone = g.Key,
                        UserCount = g.Count(),
                        Percentage = g.Count() * 100.0 / _context.DeviceTokens.Count(dt => dt.IsActive && !dt.IsDeleted)
                    })
                    .OrderByDescending(t => t.UserCount)
                    .ToListAsync();

                return Ok(new { timezones = timezoneStats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting timezone distribution");
                return StatusCode(500, new { error = "Failed to get timezone distribution" });
            }
        }

        /// <summary>
        /// Get notification type preferences
        /// </summary>
        [HttpGet("preferences")]
        public async Task<IActionResult> GetPreferenceStats()
        {
            try
            {
                var preferences = await _context.UserNotificationPreferences
                    .Select(p => new
                    {
                        Morning = p.Morning,
                        Evening = p.Evening,
                        FajrMissed = p.FajrMissed,
                        Escalation = p.Escalation,
                        HadithDaily = p.HadithDaily,
                        MotivationDaily = p.MotivationDaily,
                        EventsNew = p.EventsNew,
                        EventsReminder = p.EventsReminder
                    })
                    .ToListAsync();

                var stats = new
                {
                    TotalUsers = preferences.Count,
                    Morning = preferences.Count(p => p.Morning),
                    Evening = preferences.Count(p => p.Evening),
                    FajrMissed = preferences.Count(p => p.FajrMissed),
                    Escalation = preferences.Count(p => p.Escalation),
                    HadithDaily = preferences.Count(p => p.HadithDaily),
                    MotivationDaily = preferences.Count(p => p.MotivationDaily),
                    EventsNew = preferences.Count(p => p.EventsNew),
                    EventsReminder = preferences.Count(p => p.EventsReminder)
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting preference stats");
                return StatusCode(500, new { error = "Failed to get preference stats" });
            }
        }

        /// <summary>
        /// Get error analysis
        /// </summary>
        [HttpGet("errors")]
        public async Task<IActionResult> GetErrorAnalysis([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            try
            {
                var fromDate = from ?? DateTime.UtcNow.AddDays(-7);
                var toDate = to ?? DateTime.UtcNow;

                var errors = await _context.NotificationLogs
                    .Where(nl => nl.Result != "Sent" && nl.CreatedAt >= fromDate && nl.CreatedAt <= toDate)
                    .GroupBy(nl => nl.Error)
                    .Select(g => new
                    {
                        Error = g.Key,
                        Count = g.Count(),
                        Percentage = g.Count() * 100.0 / _context.NotificationLogs.Count(nl => nl.Result != "Sent" && nl.CreatedAt >= fromDate && nl.CreatedAt <= toDate)
                    })
                    .OrderByDescending(e => e.Count)
                    .ToListAsync();

                return Ok(new { period = new { from = fromDate, to = toDate }, errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting error analysis");
                return StatusCode(500, new { error = "Failed to get error analysis" });
            }
        }

        /// <summary>
        /// Get daily notification volume
        /// </summary>
        [HttpGet("volume/daily")]
        public async Task<IActionResult> GetDailyVolume([FromQuery] int days = 7)
        {
            try
            {
                var fromDate = DateTime.UtcNow.AddDays(-days);
                
                var dailyVolume = await _context.NotificationLogs
                    .Where(nl => nl.CreatedAt >= fromDate)
                    .GroupBy(nl => nl.CreatedAt.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Total = g.Count(),
                        Successful = g.Count(nl => nl.Result == "Sent"),
                        Failed = g.Count(nl => nl.Result != "Sent")
                    })
                    .OrderBy(v => v.Date)
                    .ToListAsync();

                return Ok(new { days, volume = dailyVolume });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily volume");
                return StatusCode(500, new { error = "Failed to get daily volume" });
            }
        }

        /// <summary>
        /// Get system performance metrics
        /// </summary>
        [HttpGet("performance")]
        public async Task<IActionResult> GetPerformanceMetrics()
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var oneHourAgo = now.AddHours(-1);

                // Get recent processing times
                var recentNotifications = await _context.ScheduledNotifications
                    .Where(sn => sn.ProcessedAt >= oneHourAgo && sn.ProcessedAt.HasValue)
                    .Select(sn => new
                    {
                        ProcessingTime = (sn.ProcessedAt!.Value - sn.ExecuteAt).TotalSeconds,
                        Type = sn.Type,
                        Success = sn.Status == "Succeeded"
                    })
                    .ToListAsync();

                var performance = new
                {
                    RecentNotifications = recentNotifications.Count,
                    AvgProcessingTime = recentNotifications.Any() ? recentNotifications.Average(n => n.ProcessingTime) : 0,
                    MaxProcessingTime = recentNotifications.Any() ? recentNotifications.Max(n => n.ProcessingTime) : 0,
                    MinProcessingTime = recentNotifications.Any() ? recentNotifications.Min(n => n.ProcessingTime) : 0,
                    SuccessRate = recentNotifications.Any() ? recentNotifications.Count(n => n.Success) * 100.0 / recentNotifications.Count : 0,
                    Timestamp = now
                };

                return Ok(performance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance metrics");
                return StatusCode(500, new { error = "Failed to get performance metrics" });
            }
        }
    }
}
