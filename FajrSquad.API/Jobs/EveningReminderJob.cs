using FajrSquad.Infrastructure.Services;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace FajrSquad.API.Jobs
{
    [DisallowConcurrentExecution]
    public class EveningReminderJob : IJob
    {
        private readonly INotificationSender _notificationSender;
        private readonly IMessageBuilder _messageBuilder;
        private readonly INotificationScheduler _notificationScheduler;
        private readonly FajrDbContext _db;
        private readonly ILogger<EveningReminderJob> _logger;

        public EveningReminderJob(
            INotificationSender notificationSender,
            IMessageBuilder messageBuilder,
            INotificationScheduler notificationScheduler,
            FajrDbContext db,
            ILogger<EveningReminderJob> logger)
        {
            _notificationSender = notificationSender;
            _messageBuilder = messageBuilder;
            _notificationScheduler = notificationScheduler;
            _db = db;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Starting EveningReminderJob execution");

                // Get all active users with their device tokens and preferences
                var users = await _db.Users
                    .Where(u => !u.IsDeleted)
                    .Include(u => u.DeviceTokens.Where(dt => dt.IsActive && !dt.IsDeleted))
                    .Include(u => u.UserNotificationPreferences)
                    .ToListAsync();

                var notificationsSent = 0;
                var currentUtc = DateTimeOffset.UtcNow;

                foreach (var user in users)
                {
                    try
                    {
                        // Check if user has evening notifications enabled
                        var preferences = user.UserNotificationPreferences?.FirstOrDefault();
                        if (preferences != null && !preferences.Evening)
                        {
                            continue;
                        }

                        // Get user's timezone
                        var timezone = user.DeviceTokens?.FirstOrDefault()?.TimeZone ?? "Africa/Dakar";
                        
                        // Skip if timezone is invalid (e.g., 'string' literal)
                        if (string.IsNullOrEmpty(timezone) || timezone == "string" || timezone.Length < 3)
                        {
                            _logger.LogWarning("Invalid timezone '{Timezone}' for user {UserId}, skipping", timezone, user.Id);
                            continue;
                        }
                        
                        TimeZoneInfo userTimeZone;
                        try
                        {
                            userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                        }
                        catch (TimeZoneNotFoundException)
                        {
                            _logger.LogWarning("Timezone '{Timezone}' not found for user {UserId}, using default", timezone, user.Id);
                            userTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Dakar");
                        }
                        
                        var userLocalTime = TimeZoneInfo.ConvertTimeFromUtc(currentUtc.DateTime, userTimeZone);

                        // Check if it's evening time for this user (around 21:30 local time)
                        if (userLocalTime.Hour == 21 && userLocalTime.Minute >= 25 && userLocalTime.Minute <= 35)
                        {
                            // Check if user hasn't already received an evening reminder today
                            var todayKey = $"evening_reminder_{user.Id}_{userLocalTime:yyyyMMdd}";
                            var existingNotification = await _db.ScheduledNotifications
                                .AnyAsync(sn => sn.UniqueKey == todayKey && sn.Status == "Sent");

                            if (!existingNotification)
                            {
                                // Schedule evening reminder for this user
                                var executeAt = currentUtc.AddMinutes(1); // Send in 1 minute
                                
                                await _notificationScheduler.ScheduleNotificationAsync(
                                    user.Id,
                                    "EveningReminder",
                                    executeAt,
                                    new { UserId = user.Id, UserLocalTime = userLocalTime },
                                    todayKey
                                );

                                notificationsSent++;
                                _logger.LogInformation("Scheduled evening reminder for user {UserId} at {ExecuteAt}", 
                                    user.Id, executeAt);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing evening reminder for user {UserId}", user.Id);
                    }
                }

                _logger.LogInformation("EveningReminderJob completed. Scheduled {Count} notifications", notificationsSent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EveningReminderJob execution");
                throw;
            }
        }
    }
}
