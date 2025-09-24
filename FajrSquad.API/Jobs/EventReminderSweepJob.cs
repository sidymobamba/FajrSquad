using FajrSquad.Infrastructure.Services;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace FajrSquad.API.Jobs
{
    [DisallowConcurrentExecution]
    public class EventReminderSweepJob : IJob
    {
        private readonly INotificationSender _notificationSender;
        private readonly IMessageBuilder _messageBuilder;
        private readonly INotificationScheduler _notificationScheduler;
        private readonly FajrDbContext _db;
        private readonly ILogger<EventReminderSweepJob> _logger;

        public EventReminderSweepJob(
            INotificationSender notificationSender,
            IMessageBuilder messageBuilder,
            INotificationScheduler notificationScheduler,
            FajrDbContext db,
            ILogger<EventReminderSweepJob> logger)
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
                _logger.LogInformation("Starting EventReminderSweepJob execution");

                var now = DateTimeOffset.UtcNow;
                var notificationsSent = 0;

                // Get all pending event reminder notifications
                var pendingReminders = await _db.ScheduledNotifications
                    .Where(sn => sn.Type == "EventReminder" && 
                                sn.Status == "Pending" && 
                                sn.ExecuteAt <= now)
                    .ToListAsync();

                foreach (var reminder in pendingReminders)
                {
                    try
                    {
                        // Mark as processing
                        reminder.Status = "Processing";
                        reminder.ProcessedAt = now;
                        await _db.SaveChangesAsync();

                        // Get the event data
                        var eventData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(reminder.DataJson);
                        if (eventData != null && eventData.TryGetValue("EventId", out var eventIdObj))
                        {
                            var eventId = Guid.Parse(eventIdObj.ToString()!);
                            var eventEntity = await _db.Events
                                .FirstOrDefaultAsync(e => e.Id == eventId);

                            if (eventEntity != null)
                            {
                                // Get all users who should receive event reminders
                                var users = await _db.Users
                                    .Where(u => !u.IsDeleted)
                                    .Include(u => u.DeviceTokens.Where(dt => dt.IsActive && !dt.IsDeleted))
                                    .Include(u => u.UserNotificationPreferences)
                                    .ToListAsync();

                                foreach (var user in users)
                                {
                                    try
                                    {
                                        // Check if user has event reminder notifications enabled
                                        var preferences = user.UserNotificationPreferences?.FirstOrDefault();
                                        if (preferences != null && !preferences.EventsReminder)
                                        {
                                            continue;
                                        }

                                        // Schedule event reminder for this user
                                        var executeAt = now.AddMinutes(1);
                                        
                                        await _notificationScheduler.ScheduleNotificationAsync(
                                            user.Id,
                                            "EventReminderUser",
                                            executeAt,
                                            new { 
                                                UserId = user.Id,
                                                EventId = eventEntity.Id,
                                                TimeUntil = eventData.GetValueOrDefault("TimeUntil", "Prossimamente")
                                            },
                                            $"event_reminder_user_{user.Id}_{eventEntity.Id}_{now:yyyyMMddHHmm}"
                                        );

                                        notificationsSent++;
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Error processing event reminder for user {UserId} and event {EventId}", 
                                            user.Id, eventEntity.Id);
                                    }
                                }
                            }
                        }

                        // Mark as sent
                        reminder.Status = "Sent";
                        await _db.SaveChangesAsync();

                        _logger.LogInformation("Processed event reminder {ReminderId}", reminder.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing event reminder {ReminderId}", reminder.Id);
                        
                        reminder.Status = "Failed";
                        reminder.ErrorMessage = ex.Message;
                        await _db.SaveChangesAsync();
                    }
                }

                _logger.LogInformation("EventReminderSweepJob completed. Processed {Count} event reminders", notificationsSent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EventReminderSweepJob execution");
                throw;
            }
        }
    }
}
