using FajrSquad.Core.Entities;
using FajrSquad.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FajrSquad.Infrastructure.Services
{
    public class NotificationMetricsService : INotificationMetricsService
    {
        private readonly FajrDbContext _db;
        private readonly ILogger<NotificationMetricsService> _logger;

        public NotificationMetricsService(FajrDbContext db, ILogger<NotificationMetricsService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task LogNotificationAsync(NotificationLog log)
        {
            try
            {
                _db.NotificationLogs.Add(log);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log notification for user {UserId}, type {Type}", log.UserId, log.Type);
            }
        }

        public async Task<NotificationMetrics> GetMetricsAsync(DateTimeOffset from, DateTimeOffset to)
        {
            try
            {
                var logs = await _db.NotificationLogs
                    .Where(nl => nl.SentAt >= from && nl.SentAt <= to)
                    .ToListAsync();

                return CalculateMetrics(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get notification metrics from {From} to {To}", from, to);
                return new NotificationMetrics();
            }
        }

        public async Task<NotificationMetrics> GetUserMetricsAsync(Guid userId, DateTimeOffset from, DateTimeOffset to)
        {
            try
            {
                var logs = await _db.NotificationLogs
                    .Where(nl => nl.UserId == userId && nl.SentAt >= from && nl.SentAt <= to)
                    .ToListAsync();

                return CalculateMetrics(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user notification metrics for {UserId} from {From} to {To}", userId, from, to);
                return new NotificationMetrics();
            }
        }

        public async Task CleanupOldLogsAsync(int daysToKeep = 30)
        {
            try
            {
                var cutoffDate = DateTimeOffset.UtcNow.AddDays(-daysToKeep);
                
                var oldLogs = await _db.NotificationLogs
                    .Where(nl => nl.SentAt < cutoffDate)
                    .ToListAsync();

                if (oldLogs.Any())
                {
                    _db.NotificationLogs.RemoveRange(oldLogs);
                    await _db.SaveChangesAsync();
                    
                    _logger.LogInformation("Cleaned up {Count} old notification logs older than {CutoffDate}", 
                        oldLogs.Count, cutoffDate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old notification logs");
            }
        }

        private NotificationMetrics CalculateMetrics(List<NotificationLog> logs)
        {
            var metrics = new NotificationMetrics();

            if (!logs.Any())
                return metrics;

            metrics.TotalSent = logs.Count(l => l.Result == "Sent");
            metrics.TotalFailed = logs.Count(l => l.Result == "Failed");
            metrics.TotalRetried = logs.Sum(l => l.Retried);

            // Success rate
            var total = logs.Count;
            metrics.SuccessRate = total > 0 ? (double)metrics.TotalSent / total * 100 : 0;

            // By type
            metrics.SentByType = logs
                .Where(l => l.Result == "Sent")
                .GroupBy(l => l.Type)
                .ToDictionary(g => g.Key, g => g.Count());

            metrics.FailedByType = logs
                .Where(l => l.Result == "Failed")
                .GroupBy(l => l.Type)
                .ToDictionary(g => g.Key, g => g.Count());

            // By hour
            metrics.SentByHour = logs
                .Where(l => l.Result == "Sent")
                .GroupBy(l => l.SentAt.Hour)
                .ToDictionary(g => g.Key.ToString("D2"), g => g.Count());

            // Top errors
            metrics.TopErrors = logs
                .Where(l => !string.IsNullOrEmpty(l.Error))
                .GroupBy(l => l.Error)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => $"{g.Key} ({g.Count()} times)")
                .ToList();

            return metrics;
        }

        public async Task<object> GetSystemHealthAsync()
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var oneHourAgo = now.AddHours(-1);

                // Check for stuck notifications
                var stuckNotifications = await _db.ScheduledNotifications
                    .CountAsync(sn => sn.Status == "Pending" && sn.ExecuteAt < oneHourAgo);

                // Check recent failure rate
                var recentNotifications = await _db.NotificationLogs
                    .Where(nl => nl.CreatedAt >= oneHourAgo)
                    .ToListAsync();

                var totalRecent = recentNotifications.Count;
                var failedRecent = recentNotifications.Count(nl => nl.Result != "Sent");
                var failureRate = totalRecent > 0 ? (double)failedRecent / totalRecent : 0;

                // Check active users
                var activeUsers = await _db.Users
                    .CountAsync(u => !u.IsDeleted && u.DeviceTokens.Any(dt => dt.IsActive && !dt.IsDeleted));

                return new
                {
                    status = stuckNotifications > 100 ? "unhealthy" : failureRate > 0.5 ? "degraded" : "healthy",
                    stuck_notifications = stuckNotifications,
                    recent_notifications = totalRecent,
                    recent_failures = failedRecent,
                    failure_rate = Math.Round(failureRate * 100, 2),
                    active_users = activeUsers,
                    timestamp = now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system health");
                return new { status = "unhealthy", error = ex.Message };
            }
        }

        public async Task<object> GetNotificationStatsAsync(DateTime from, DateTime to)
        {
            try
            {
                var fromOffset = new DateTimeOffset(from);
                var toOffset = new DateTimeOffset(to);

                var logs = await _db.NotificationLogs
                    .Where(nl => nl.CreatedAt >= fromOffset && nl.CreatedAt <= toOffset)
                    .ToListAsync();

                var total = logs.Count;
                var successful = logs.Count(l => l.Result == "Sent");
                var failed = logs.Count(l => l.Result == "Failed");

                var byType = logs
                    .GroupBy(l => l.Type)
                    .Select(g => new
                    {
                        type = g.Key,
                        total = g.Count(),
                        successful = g.Count(l => l.Result == "Sent"),
                        failed = g.Count(l => l.Result == "Failed"),
                        success_rate = g.Count() > 0 ? Math.Round(g.Count(l => l.Result == "Sent") * 100.0 / g.Count(), 2) : 0
                    })
                    .ToList();

                return new
                {
                    period = new { from, to },
                    total,
                    successful,
                    failed,
                    success_rate = total > 0 ? Math.Round(successful * 100.0 / total, 2) : 0,
                    by_type = byType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification stats");
                throw;
            }
        }
    }
}
