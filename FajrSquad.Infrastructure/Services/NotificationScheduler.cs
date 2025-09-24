using FajrSquad.Core.Entities;
using FajrSquad.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FajrSquad.Infrastructure.Services
{
    public class NotificationScheduler : INotificationScheduler
    {
        private readonly FajrDbContext _db;
        private readonly ILogger<NotificationScheduler> _logger;
        private readonly INotificationPrivacyService _privacyService;

        public NotificationScheduler(
            FajrDbContext db, 
            ILogger<NotificationScheduler> logger,
            INotificationPrivacyService privacyService)
        {
            _db = db;
            _logger = logger;
            _privacyService = privacyService;
        }

        public async Task ScheduleNotificationAsync(Guid? userId, string type, DateTimeOffset executeAt, object data, string? uniqueKey = null)
        {
            try
            {
                // Check if notification with same unique key already exists
                if (!string.IsNullOrEmpty(uniqueKey))
                {
                    var existing = await _db.ScheduledNotifications
                        .FirstOrDefaultAsync(sn => sn.UniqueKey == uniqueKey && sn.Status == "Pending");
                    
                    if (existing != null)
                    {
                        _logger.LogInformation("Notification with unique key {UniqueKey} already exists, skipping", uniqueKey);
                        return;
                    }
                }

                // Check privacy controls for user-specific notifications
                if (userId.HasValue)
                {
                    var shouldSend = await _privacyService.ShouldSendNotificationAsync(userId.Value, type, executeAt);
                    if (!shouldSend)
                    {
                        _logger.LogInformation("Notification {Type} blocked by privacy controls for user {UserId}", type, userId);
                        return;
                    }
                }

                var scheduledNotification = new ScheduledNotification
                {
                    UserId = userId,
                    Type = type,
                    ExecuteAt = executeAt,
                    DataJson = JsonSerializer.Serialize(data),
                    Status = "Pending",
                    UniqueKey = uniqueKey
                };

                _db.ScheduledNotifications.Add(scheduledNotification);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Scheduled notification {Type} for user {UserId} at {ExecuteAt}", 
                    type, userId, executeAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to schedule notification {Type} for user {UserId}", type, userId);
                throw;
            }
        }

        public async Task ScheduleEventRemindersAsync(Event eventEntity)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var eventStart = eventEntity.StartDate;

                // Schedule 24 hours before
                var reminder24h = eventStart.AddHours(-24);
                if (reminder24h > now)
                {
                    await ScheduleNotificationAsync(
                        null, // Broadcast to all users
                        "EventReminder",
                        reminder24h,
                        new { EventId = eventEntity.Id, TimeUntil = "Domani" },
                        $"event_reminder_24h_{eventEntity.Id}"
                    );
                }

                // Schedule 2 hours before
                var reminder2h = eventStart.AddHours(-2);
                if (reminder2h > now)
                {
                    await ScheduleNotificationAsync(
                        null, // Broadcast to all users
                        "EventReminder",
                        reminder2h,
                        new { EventId = eventEntity.Id, TimeUntil = "Tra 2 ore" },
                        $"event_reminder_2h_{eventEntity.Id}"
                    );
                }

                _logger.LogInformation("Scheduled event reminders for event {EventId}", eventEntity.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to schedule event reminders for event {EventId}", eventEntity.Id);
                throw;
            }
        }

        public async Task ProcessScheduledNotificationsAsync()
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var pendingNotifications = await GetPendingNotificationsAsync(now);

                _logger.LogInformation("Processing {Count} pending notifications", pendingNotifications.Count);

                foreach (var notification in pendingNotifications)
                {
                    try
                    {
                        // Mark as processing
                        notification.Status = "Processing";
                        notification.ProcessedAt = now;
                        await _db.SaveChangesAsync();

                        // Process the notification based on type
                        await ProcessNotificationAsync(notification);

                        // Mark as sent
                        notification.Status = "Sent";
                        await _db.SaveChangesAsync();

                        _logger.LogInformation("Successfully processed notification {Id} of type {Type}", 
                            notification.Id, notification.Type);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process notification {Id} of type {Type}", 
                            notification.Id, notification.Type);
                        
                        notification.Status = "Failed";
                        notification.ErrorMessage = ex.Message;
                        await _db.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process scheduled notifications");
                throw;
            }
        }

        public async Task CancelScheduledNotificationAsync(string uniqueKey)
        {
            try
            {
                var notifications = await _db.ScheduledNotifications
                    .Where(sn => sn.UniqueKey == uniqueKey && sn.Status == "Pending")
                    .ToListAsync();

                foreach (var notification in notifications)
                {
                    notification.Status = "Cancelled";
                    notification.ProcessedAt = DateTimeOffset.UtcNow;
                }

                await _db.SaveChangesAsync();
                _logger.LogInformation("Cancelled {Count} notifications with unique key {UniqueKey}", 
                    notifications.Count, uniqueKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel notifications with unique key {UniqueKey}", uniqueKey);
                throw;
            }
        }

        public async Task<List<ScheduledNotification>> GetPendingNotificationsAsync(DateTimeOffset? before = null)
        {
            var query = _db.ScheduledNotifications
                .Where(sn => sn.Status == "Pending" && sn.ExecuteAt <= (before ?? DateTimeOffset.UtcNow))
                .OrderBy(sn => sn.ExecuteAt);

            return await query.ToListAsync();
        }

        private async Task ProcessNotificationAsync(ScheduledNotification notification)
        {
            // This method would integrate with the notification sender
            // For now, we'll just log the notification
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(notification.DataJson);
            
            _logger.LogInformation("Processing notification {Type} with data: {Data}", 
                notification.Type, notification.DataJson);

            // TODO: Integrate with INotificationSender to actually send the notification
            // This would involve:
            // 1. Deserializing the data
            // 2. Building the appropriate notification request
            // 3. Sending via FCM
            // 4. Logging the result
        }
    }
}
