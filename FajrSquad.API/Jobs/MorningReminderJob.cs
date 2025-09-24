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
        private readonly IConfiguration _configuration;
        private readonly ITimezoneService _timezoneService;

        public MorningReminderJob(
            INotificationSender notificationSender,
            IMessageBuilder messageBuilder,
            INotificationScheduler notificationScheduler,
            FajrDbContext db,
            ILogger<MorningReminderJob> logger,
            IConfiguration configuration,
            ITimezoneService timezoneService)
        {
            _notificationSender = notificationSender;
            _messageBuilder = messageBuilder;
            _notificationScheduler = notificationScheduler;
            _db = db;
            _logger = logger;
            _configuration = configuration;
            _timezoneService = timezoneService;
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

                        // Get user's timezone and normalize it
                        var rawTimezone = user.DeviceTokens?.FirstOrDefault()?.TimeZone;
                        var timezone = _timezoneService.NormalizeTimezone(rawTimezone);
                        
                        // Get ForceWindow setting for Development
                        var force = _configuration.GetValue<bool>("Notifications:ForceWindow", false);
                        
                        // Helper function to check if within time window
                        bool WithinWindow(TimeSpan now, TimeSpan target, int toleranceMinutes) =>
                            Math.Abs((now - target).TotalMinutes) <= toleranceMinutes;

                        var nowLocal = _timezoneService.GetCurrentLocalTime(timezone).TimeOfDay;
                        var target = TimeSpan.Parse(_configuration["Notifications:MorningTime"] ?? "06:30");
                        var tolerance = _configuration.GetValue<int>("Notifications:MorningToleranceMinutes", 5);

                        var shouldSend = (preferences?.Morning ?? true) && (force || WithinWindow(nowLocal, target, tolerance));
                        
                        _logger.LogInformation("MorningCheck user={UserId} nowLocal={NowLocal} target={Target} tolerance={Tolerance}m force={Force} send={ShouldSend}",
                            user.Id, nowLocal, target, tolerance, force, shouldSend);

                        if (shouldSend)
                        {
                            // Check if user hasn't already received a morning reminder today
                            var todayKey = $"morning_reminder_{user.Id}_{currentUtc:yyyyMMdd}";
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
                                    new { UserId = user.Id, UserLocalTime = nowLocal },
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
