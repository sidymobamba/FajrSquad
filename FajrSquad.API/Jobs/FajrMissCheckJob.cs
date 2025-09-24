using FajrSquad.Infrastructure.Services;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace FajrSquad.API.Jobs
{
    [DisallowConcurrentExecution]
    public class FajrMissCheckJob : IJob
    {
        private readonly INotificationSender _notificationSender;
        private readonly IMessageBuilder _messageBuilder;
        private readonly INotificationScheduler _notificationScheduler;
        private readonly FajrDbContext _db;
        private readonly ILogger<FajrMissCheckJob> _logger;

        public FajrMissCheckJob(
            INotificationSender notificationSender,
            IMessageBuilder messageBuilder,
            INotificationScheduler notificationScheduler,
            FajrDbContext db,
            ILogger<FajrMissCheckJob> logger)
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
                _logger.LogInformation("Starting FajrMissCheckJob execution");

                var today = DateTime.UtcNow.Date;
                var notificationsSent = 0;
                var adminAlertsSent = 0;

                // Get all active users with their device tokens and preferences
                var users = await _db.Users
                    .Where(u => !u.IsDeleted)
                    .Include(u => u.DeviceTokens.Where(dt => dt.IsActive && !dt.IsDeleted))
                    .Include(u => u.UserNotificationPreferences)
                    .Include(u => u.FajrCheckIns.Where(fc => fc.Date >= today.AddDays(-7))) // Last 7 days
                    .ToListAsync();

                foreach (var user in users)
                {
                    try
                    {
                        // Check if user has Fajr missed notifications enabled
                        var preferences = user.UserNotificationPreferences?.FirstOrDefault();
                        if (preferences != null && !preferences.FajrMissed)
                        {
                            continue;
                        }

                        // Check if user hasn't checked in today
                        var hasCheckedInToday = user.FajrCheckIns?.Any(fc => fc.Date == today) ?? false;
                        
                        if (!hasCheckedInToday)
                        {
                            // Count consecutive missed days
                            var consecutiveMissedDays = 0;
                            var checkDate = today;
                            
                            while (checkDate >= today.AddDays(-30)) // Check last 30 days max
                            {
                                var hasCheckedIn = user.FajrCheckIns?.Any(fc => fc.Date == checkDate) ?? false;
                                if (hasCheckedIn)
                                    break;
                                
                                consecutiveMissedDays++;
                                checkDate = checkDate.AddDays(-1);
                            }

                            // Send late motivation notification
                            var todayKey = $"fajr_late_motivation_{user.Id}_{today:yyyyMMdd}";
                            var existingNotification = await _db.ScheduledNotifications
                                .AnyAsync(sn => sn.UniqueKey == todayKey && sn.Status == "Sent");

                            if (!existingNotification)
                            {
                                var executeAt = DateTimeOffset.UtcNow.AddMinutes(1);
                                
                                await _notificationScheduler.ScheduleNotificationAsync(
                                    user.Id,
                                    "FajrLateMotivation",
                                    executeAt,
                                    new { 
                                        UserId = user.Id, 
                                        ConsecutiveMissedDays = consecutiveMissedDays,
                                        FajrTime = "05:30" // TODO: Get actual Fajr time from prayer service
                                    },
                                    todayKey
                                );

                                notificationsSent++;
                                _logger.LogInformation("Scheduled Fajr late motivation for user {UserId} with {Days} consecutive missed days", 
                                    user.Id, consecutiveMissedDays);
                            }

                            // Check if user has missed 3+ consecutive days and send admin alert
                            if (consecutiveMissedDays >= 3)
                            {
                                var adminAlertKey = $"admin_alert_{user.Id}_{today:yyyyMMdd}";
                                var existingAdminAlert = await _db.ScheduledNotifications
                                    .AnyAsync(sn => sn.UniqueKey == adminAlertKey && sn.Status == "Sent");

                                if (!existingAdminAlert)
                                {
                                    var executeAt = DateTimeOffset.UtcNow.AddMinutes(1);
                                    
                                    await _notificationScheduler.ScheduleNotificationAsync(
                                        null, // Send to admins
                                        "AdminAlert",
                                        executeAt,
                                        new { 
                                            UserId = user.Id,
                                            UserName = user.Name,
                                            UserCity = user.City,
                                            ConsecutiveMissedDays = consecutiveMissedDays
                                        },
                                        adminAlertKey
                                    );

                                    adminAlertsSent++;
                                    _logger.LogWarning("Scheduled admin alert for user {UserId} with {Days} consecutive missed days", 
                                        user.Id, consecutiveMissedDays);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing Fajr miss check for user {UserId}", user.Id);
                    }
                }

                _logger.LogInformation("FajrMissCheckJob completed. Scheduled {Count} late motivations and {AdminCount} admin alerts", 
                    notificationsSent, adminAlertsSent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FajrMissCheckJob execution");
                throw;
            }
        }
    }
}
