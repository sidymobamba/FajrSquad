using FajrSquad.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace FajrSquad.Infrastructure.HealthChecks
{
    public class NotificationHealthCheck : IHealthCheck
    {
        private readonly FajrDbContext _context;
        private readonly ILogger<NotificationHealthCheck> _logger;

        public NotificationHealthCheck(FajrDbContext context, ILogger<NotificationHealthCheck> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var oneHourAgo = now.AddHours(-1);

                // Check database connectivity
                await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);

                // Check for stuck notifications (pending for more than 1 hour)
                var stuckNotifications = await _context.ScheduledNotifications
                    .CountAsync(sn => sn.Status == "Pending" && sn.ExecuteAt < oneHourAgo, cancellationToken);

                // Check for high failure rate in the last hour
                var recentNotifications = await _context.NotificationLogs
                    .Where(nl => nl.CreatedAt >= oneHourAgo)
                    .ToListAsync(cancellationToken);

                var totalRecent = recentNotifications.Count;
                var failedRecent = recentNotifications.Count(nl => nl.Result != "Sent");
                var failureRate = totalRecent > 0 ? (double)failedRecent / totalRecent : 0;

                // Check for active users with device tokens
                var activeUsers = await _context.Users
                    .CountAsync(u => !u.IsDeleted && u.DeviceTokens.Any(dt => dt.IsActive && !dt.IsDeleted), cancellationToken);

                var data = new Dictionary<string, object>
                {
                    ["stuck_notifications"] = stuckNotifications,
                    ["recent_notifications"] = totalRecent,
                    ["recent_failures"] = failedRecent,
                    ["failure_rate"] = Math.Round(failureRate * 100, 2),
                    ["active_users"] = activeUsers,
                    ["timestamp"] = now
                };

                // Determine health status
                if (stuckNotifications > 100)
                {
                    return HealthCheckResult.Unhealthy(
                        $"Too many stuck notifications: {stuckNotifications}",
                        null,
                        data);
                }

                if (failureRate > 0.5) // More than 50% failure rate
                {
                    return HealthCheckResult.Degraded(
                        $"High failure rate: {Math.Round(failureRate * 100, 2)}%",
                        null,
                        data);
                }

                if (activeUsers == 0)
                {
                    return HealthCheckResult.Degraded(
                        "No active users with device tokens",
                        null,
                        data);
                }

                return HealthCheckResult.Healthy("Notification system is healthy", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification health check failed");
                return HealthCheckResult.Unhealthy("Notification system health check failed", ex);
            }
        }
    }
}
