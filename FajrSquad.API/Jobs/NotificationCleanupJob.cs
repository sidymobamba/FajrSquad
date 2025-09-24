using FajrSquad.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

namespace FajrSquad.API.Jobs
{
    [DisallowConcurrentExecution]
    public class NotificationCleanupJob : IJob
    {
        private readonly FajrDbContext _db;
        private readonly ILogger<NotificationCleanupJob> _logger;
        private readonly IConfiguration _configuration;

        public NotificationCleanupJob(
            FajrDbContext db,
            ILogger<NotificationCleanupJob> logger,
            IConfiguration configuration)
        {
            _db = db;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Starting NotificationCleanupJob execution");

                var logsRetentionDays = _configuration.GetValue<int>("Notifications:LogsRetentionDays", 30);
                var scheduledRetentionDays = _configuration.GetValue<int>("Notifications:ScheduledRetentionDays", 7);

                var logsCutoffDate = DateTime.UtcNow.AddDays(-logsRetentionDays);
                var scheduledCutoffDate = DateTime.UtcNow.AddDays(-scheduledRetentionDays);

                // Clean up old notification logs
                var deletedLogs = await _db.NotificationLogs
                    .Where(nl => nl.CreatedAt < logsCutoffDate)
                    .ExecuteDeleteAsync();

                _logger.LogInformation("Cleaned up {Count} old notification logs (older than {Days} days)", 
                    deletedLogs, logsRetentionDays);

                // Clean up old processed scheduled notifications
                var deletedScheduled = await _db.ScheduledNotifications
                    .Where(sn => sn.ProcessedAt.HasValue && 
                                sn.ProcessedAt < scheduledCutoffDate &&
                                (sn.Status == "Succeeded" || sn.Status == "Failed" || sn.Status == "Cancelled"))
                    .ExecuteDeleteAsync();

                _logger.LogInformation("Cleaned up {Count} old scheduled notifications (older than {Days} days)", 
                    deletedScheduled, scheduledRetentionDays);

                // Clean up orphaned scheduled notifications (no user)
                var orphanedCount = await _db.ScheduledNotifications
                    .Where(sn => sn.UserId.HasValue && 
                                !_db.Users.Any(u => u.Id == sn.UserId.Value && !u.IsDeleted))
                    .ExecuteDeleteAsync();

                if (orphanedCount > 0)
                {
                    _logger.LogWarning("Cleaned up {Count} orphaned scheduled notifications", orphanedCount);
                }

                // Clean up inactive device tokens (older than 90 days)
                var inactiveTokensCutoff = DateTime.UtcNow.AddDays(-90);
                var deletedTokens = await _db.DeviceTokens
                    .Where(dt => !dt.IsActive && dt.UpdatedAt < inactiveTokensCutoff)
                    .ExecuteDeleteAsync();

                if (deletedTokens > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} inactive device tokens (older than 90 days)", deletedTokens);
                }

                _logger.LogInformation("NotificationCleanupJob completed successfully. " +
                    "Logs: {Logs}, Scheduled: {Scheduled}, Orphaned: {Orphaned}, Tokens: {Tokens}",
                    deletedLogs, deletedScheduled, orphanedCount, deletedTokens);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NotificationCleanupJob execution");
                throw;
            }
        }
    }
}