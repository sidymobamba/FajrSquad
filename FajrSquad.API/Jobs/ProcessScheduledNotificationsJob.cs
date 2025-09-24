using FajrSquad.Infrastructure.Services;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace FajrSquad.API.Jobs
{
    [DisallowConcurrentExecution]
    public class ProcessScheduledNotificationsJob : IJob
    {
        private readonly INotificationSender _notificationSender;
        private readonly IMessageBuilder _messageBuilder;
        private readonly INotificationScheduler _notificationScheduler;
        private readonly FajrDbContext _db;
        private readonly ILogger<ProcessScheduledNotificationsJob> _logger;

        public ProcessScheduledNotificationsJob(
            INotificationSender notificationSender,
            IMessageBuilder messageBuilder,
            INotificationScheduler notificationScheduler,
            FajrDbContext db,
            ILogger<ProcessScheduledNotificationsJob> logger)
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
                _logger.LogInformation("Starting ProcessScheduledNotificationsJob execution");

                var now = DateTimeOffset.UtcNow;
                var notificationsProcessed = 0;
                var notificationsFailed = 0;

                // Get all pending notifications that should be executed now
                var pendingNotifications = await _db.ScheduledNotifications
                    .Where(sn => sn.Status == "Pending" && sn.ExecuteAt <= now)
                    .OrderBy(sn => sn.ExecuteAt)
                    .Take(100) // Process max 100 at a time to avoid overwhelming the system
                    .ToListAsync();

                foreach (var notification in pendingNotifications)
                {
                    try
                    {
                        // Mark as processing
                        notification.Status = "Processing";
                        notification.ProcessedAt = now;
                        await _db.SaveChangesAsync();

                        // Process the notification based on type
                        var result = await ProcessNotificationAsync(notification);
                        
                        if (result.Success)
                        {
                            notification.Status = "Sent";
                            notificationsProcessed++;
                            _logger.LogInformation("Successfully processed notification {Id} of type {Type}", 
                                notification.Id, notification.Type);
                        }
                        else
                        {
                            notification.Status = "Failed";
                            notification.ErrorMessage = result.Error;
                            notificationsFailed++;
                            _logger.LogError("Failed to process notification {Id} of type {Type}: {Error}", 
                                notification.Id, notification.Type, result.Error);
                        }

                        await _db.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing notification {Id} of type {Type}", 
                            notification.Id, notification.Type);
                        
                        notification.Status = "Failed";
                        notification.ErrorMessage = ex.Message;
                        notificationsFailed++;
                        await _db.SaveChangesAsync();
                    }
                }

                _logger.LogInformation("ProcessScheduledNotificationsJob completed. Processed {Processed} notifications, {Failed} failed", 
                    notificationsProcessed, notificationsFailed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessScheduledNotificationsJob execution");
                throw;
            }
        }

        private async Task<NotificationResult> ProcessNotificationAsync(ScheduledNotification notification)
        {
            try
            {
                var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(notification.DataJson);
                if (data == null)
                {
                    return new NotificationResult
                    {
                        Success = false,
                        Error = "Invalid notification data"
                    };
                }

                switch (notification.Type)
                {
                    case "MorningReminder":
                        return await ProcessMorningReminderAsync(notification, data);
                    
                    case "EveningReminder":
                        return await ProcessEveningReminderAsync(notification, data);
                    
                    case "FajrLateMotivation":
                        return await ProcessFajrLateMotivationAsync(notification, data);
                    
                    case "EscalationReminder":
                        return await ProcessEscalationReminderAsync(notification, data);
                    
                    case "AdminAlert":
                        return await ProcessAdminAlertAsync(notification, data);
                    
                    case "DailyHadith":
                        return await ProcessDailyHadithAsync(notification, data);
                    
                    case "DailyMotivation":
                        return await ProcessDailyMotivationAsync(notification, data);
                    
                    case "EventReminderUser":
                        return await ProcessEventReminderAsync(notification, data);
                    
                    default:
                        return new NotificationResult
                        {
                            Success = false,
                            Error = $"Unknown notification type: {notification.Type}"
                        };
                }
            }
            catch (Exception ex)
            {
                return new NotificationResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        private async Task<NotificationResult> ProcessMorningReminderAsync(ScheduledNotification notification, Dictionary<string, object> data)
        {
            if (!data.TryGetValue("UserId", out var userIdObj))
                return new NotificationResult { Success = false, Error = "Missing UserId" };

            var userId = Guid.Parse(userIdObj.ToString()!);
            var user = await _db.Users
                .Include(u => u.DeviceTokens.Where(dt => dt.IsActive && !dt.IsDeleted))
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.DeviceTokens?.FirstOrDefault() is not DeviceToken deviceToken)
                return new NotificationResult { Success = false, Error = "User or device token not found" };

            var request = await _messageBuilder.BuildMorningReminderAsync(user, deviceToken);
            return await _notificationSender.SendToUserAsync(userId, request);
        }

        private async Task<NotificationResult> ProcessEveningReminderAsync(ScheduledNotification notification, Dictionary<string, object> data)
        {
            if (!data.TryGetValue("UserId", out var userIdObj))
                return new NotificationResult { Success = false, Error = "Missing UserId" };

            var userId = Guid.Parse(userIdObj.ToString()!);
            var user = await _db.Users
                .Include(u => u.DeviceTokens.Where(dt => dt.IsActive && !dt.IsDeleted))
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.DeviceTokens?.FirstOrDefault() is not DeviceToken deviceToken)
                return new NotificationResult { Success = false, Error = "User or device token not found" };

            var request = await _messageBuilder.BuildEveningReminderAsync(user, deviceToken);
            return await _notificationSender.SendToUserAsync(userId, request);
        }

        private async Task<NotificationResult> ProcessFajrLateMotivationAsync(ScheduledNotification notification, Dictionary<string, object> data)
        {
            if (!data.TryGetValue("UserId", out var userIdObj))
                return new NotificationResult { Success = false, Error = "Missing UserId" };

            var userId = Guid.Parse(userIdObj.ToString()!);
            var user = await _db.Users
                .Include(u => u.DeviceTokens.Where(dt => dt.IsActive && !dt.IsDeleted))
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.DeviceTokens?.FirstOrDefault() is not DeviceToken deviceToken)
                return new NotificationResult { Success = false, Error = "User or device token not found" };

            var fajrTime = TimeSpan.Parse("05:30"); // TODO: Get actual Fajr time
            var request = await _messageBuilder.BuildFajrLateMotivationAsync(user, deviceToken, fajrTime);
            return await _notificationSender.SendToUserAsync(userId, request);
        }

        private async Task<NotificationResult> ProcessEscalationReminderAsync(ScheduledNotification notification, Dictionary<string, object> data)
        {
            if (!data.TryGetValue("UserId", out var userIdObj))
                return new NotificationResult { Success = false, Error = "Missing UserId" };

            var userId = Guid.Parse(userIdObj.ToString()!);
            var user = await _db.Users
                .Include(u => u.DeviceTokens.Where(dt => dt.IsActive && !dt.IsDeleted))
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.DeviceTokens?.FirstOrDefault() is not DeviceToken deviceToken)
                return new NotificationResult { Success = false, Error = "User or device token not found" };

            var request = await _messageBuilder.BuildEscalationReminderAsync(user, deviceToken);
            return await _notificationSender.SendToUserAsync(userId, request);
        }

        private async Task<NotificationResult> ProcessAdminAlertAsync(ScheduledNotification notification, Dictionary<string, object> data)
        {
            if (!data.TryGetValue("UserId", out var userIdObj) || 
                !data.TryGetValue("ConsecutiveMissedDays", out var daysObj))
                return new NotificationResult { Success = false, Error = "Missing required data" };

            var userId = Guid.Parse(userIdObj.ToString()!);
            var consecutiveDays = Convert.ToInt32(daysObj);
            
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return new NotificationResult { Success = false, Error = "User not found" };

            var request = await _messageBuilder.BuildAdminAlertAsync(user, consecutiveDays);
            
            // Send to admin users (for now, send to a topic)
            return await _notificationSender.SendToTopicAsync("admin_alerts", request);
        }

        private async Task<NotificationResult> ProcessDailyHadithAsync(ScheduledNotification notification, Dictionary<string, object> data)
        {
            if (!data.TryGetValue("UserId", out var userIdObj) || 
                !data.TryGetValue("HadithId", out var hadithIdObj))
                return new NotificationResult { Success = false, Error = "Missing required data" };

            var userId = Guid.Parse(userIdObj.ToString()!);
            var hadithId = Guid.Parse(hadithIdObj.ToString()!);
            
            var user = await _db.Users
                .Include(u => u.DeviceTokens.Where(dt => dt.IsActive && !dt.IsDeleted))
                .FirstOrDefaultAsync(u => u.Id == userId);

            var hadith = await _db.Hadiths.FirstOrDefaultAsync(h => h.Id == hadithId);

            if (user?.DeviceTokens?.FirstOrDefault() is not DeviceToken deviceToken || hadith == null)
                return new NotificationResult { Success = false, Error = "User, device token, or hadith not found" };

            var request = await _messageBuilder.BuildDailyHadithAsync(hadith, user, deviceToken);
            return await _notificationSender.SendToUserAsync(userId, request);
        }

        private async Task<NotificationResult> ProcessDailyMotivationAsync(ScheduledNotification notification, Dictionary<string, object> data)
        {
            if (!data.TryGetValue("UserId", out var userIdObj) || 
                !data.TryGetValue("MotivationId", out var motivationIdObj))
                return new NotificationResult { Success = false, Error = "Missing required data" };

            var userId = Guid.Parse(userIdObj.ToString()!);
            var motivationId = Guid.Parse(motivationIdObj.ToString()!);
            
            var user = await _db.Users
                .Include(u => u.DeviceTokens.Where(dt => dt.IsActive && !dt.IsDeleted))
                .FirstOrDefaultAsync(u => u.Id == userId);

            var motivation = await _db.Motivations.FirstOrDefaultAsync(m => m.Id == motivationId);

            if (user?.DeviceTokens?.FirstOrDefault() is not DeviceToken deviceToken || motivation == null)
                return new NotificationResult { Success = false, Error = "User, device token, or motivation not found" };

            var request = await _messageBuilder.BuildDailyMotivationAsync(motivation, user, deviceToken);
            return await _notificationSender.SendToUserAsync(userId, request);
        }

        private async Task<NotificationResult> ProcessEventReminderAsync(ScheduledNotification notification, Dictionary<string, object> data)
        {
            if (!data.TryGetValue("UserId", out var userIdObj) || 
                !data.TryGetValue("EventId", out var eventIdObj) ||
                !data.TryGetValue("TimeUntil", out var timeUntilObj))
                return new NotificationResult { Success = false, Error = "Missing required data" };

            var userId = Guid.Parse(userIdObj.ToString()!);
            var eventId = Guid.Parse(eventIdObj.ToString()!);
            var timeUntil = timeUntilObj.ToString()!;
            
            var user = await _db.Users
                .Include(u => u.DeviceTokens.Where(dt => dt.IsActive && !dt.IsDeleted))
                .FirstOrDefaultAsync(u => u.Id == userId);

            var eventEntity = await _db.Events.FirstOrDefaultAsync(e => e.Id == eventId);

            if (user?.DeviceTokens?.FirstOrDefault() is not DeviceToken deviceToken || eventEntity == null)
                return new NotificationResult { Success = false, Error = "User, device token, or event not found" };

            var request = await _messageBuilder.BuildEventReminderAsync(eventEntity, user, deviceToken, timeUntil);
            return await _notificationSender.SendToUserAsync(userId, request);
        }
    }
}
