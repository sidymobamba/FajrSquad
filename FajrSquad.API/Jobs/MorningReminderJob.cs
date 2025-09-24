using FajrSquad.Infrastructure.Services;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using System.Globalization;

namespace FajrSquad.API.Jobs
{
    [DisallowConcurrentExecution]
    public class MorningReminderJob : IJob
    {
        private readonly INotificationSender _notificationSender;
        private readonly IMessageBuilder _messageBuilder;
        private readonly INotificationScheduler _notificationScheduler;
        private readonly FajrDbContext _db;
        private readonly ILogger<MorningReminderJob> _logger;

        public MorningReminderJob(
            INotificationSender notificationSender,
            IMessageBuilder messageBuilder,
            INotificationScheduler notificationScheduler,
            FajrDbContext db,
            ILogger<MorningReminderJob> logger)
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
                _logger.LogInformation("Starting MorningReminderJob execution");

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
                        // Check if user has morning notifications enabled
                        var preferences = user.UserNotificationPreferences?.FirstOrDefault();
                        if (preferences != null && !preferences.Morning)
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

                        // Check if it's morning time for this user (between 5:00 and 8:00 AM local time)
                        if (userLocalTime.Hour >= 5 && userLocalTime.Hour < 8)
                        {
                            // Check if user hasn't already received a morning reminder today
                            var todayKey = $"morning_reminder_{user.Id}_{userLocalTime:yyyyMMdd}";
                            var existingNotification = await _db.ScheduledNotifications
                                .AnyAsync(sn => sn.UniqueKey == todayKey && sn.Status == "Sent");

                            if (!existingNotification)
                            {
                                // Schedule morning reminder for this user
                                var executeAt = currentUtc.AddMinutes(1); // Send in 1 minute
                                
                                await _notificationScheduler.ScheduleNotificationAsync(
                                    user.Id,
                                    "MorningReminder",
                                    executeAt,
                                    new { UserId = user.Id, UserLocalTime = userLocalTime },
                                    todayKey
                                );

                                notificationsSent++;
                                _logger.LogInformation("Scheduled morning reminder for user {UserId} at {ExecuteAt}", 
                                    user.Id, executeAt);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing morning reminder for user {UserId}", user.Id);
                    }
                }

                _logger.LogInformation("MorningReminderJob completed. Scheduled {Count} notifications", notificationsSent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MorningReminderJob execution");
                throw;
            }
        }
    }
}
