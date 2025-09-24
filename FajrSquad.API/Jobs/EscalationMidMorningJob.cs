using FajrSquad.Infrastructure.Services;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace FajrSquad.API.Jobs
{
    [DisallowConcurrentExecution]
    public class EscalationMidMorningJob : IJob
    {
        private readonly INotificationSender _notificationSender;
        private readonly IMessageBuilder _messageBuilder;
        private readonly INotificationScheduler _notificationScheduler;
        private readonly FajrDbContext _db;
        private readonly ILogger<EscalationMidMorningJob> _logger;

        public EscalationMidMorningJob(
            INotificationSender notificationSender,
            IMessageBuilder messageBuilder,
            INotificationScheduler notificationScheduler,
            FajrDbContext db,
            ILogger<EscalationMidMorningJob> logger)
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
                _logger.LogInformation("Starting EscalationMidMorningJob execution");

                var today = DateTime.UtcNow.Date;
                var notificationsSent = 0;

                // Get all active users with their device tokens and preferences
                var users = await _db.Users
                    .Where(u => !u.IsDeleted)
                    .Include(u => u.DeviceTokens.Where(dt => dt.IsActive && !dt.IsDeleted))
                    .Include(u => u.UserNotificationPreferences)
                    .Include(u => u.FajrCheckIns.Where(fc => fc.Date == today))
                    .ToListAsync();

                foreach (var user in users)
                {
                    try
                    {
                        // Check if user has escalation notifications enabled
                        var preferences = user.UserNotificationPreferences?.FirstOrDefault();
                        if (preferences != null && !preferences.Escalation)
                        {
                            continue;
                        }

                        // Check if user hasn't checked in today
                        var hasCheckedInToday = user.FajrCheckIns?.Any(fc => fc.Date == today) ?? false;
                        
                        if (!hasCheckedInToday)
                        {
                            // Check if user hasn't already received an escalation reminder today
                            var todayKey = $"escalation_reminder_{user.Id}_{today:yyyyMMdd}";
                            var existingNotification = await _db.ScheduledNotifications
                                .AnyAsync(sn => sn.UniqueKey == todayKey && sn.Status == "Sent");

                            if (!existingNotification)
                            {
                                // Schedule escalation reminder for this user
                                var executeAt = DateTimeOffset.UtcNow.AddMinutes(1);
                                
                                await _notificationScheduler.ScheduleNotificationAsync(
                                    user.Id,
                                    "EscalationReminder",
                                    executeAt,
                                    new { 
                                        UserId = user.Id,
                                        MissedDays = 1 // TODO: Calculate actual missed days
                                    },
                                    todayKey
                                );

                                notificationsSent++;
                                _logger.LogInformation("Scheduled escalation reminder for user {UserId} at {ExecuteAt}", 
                                    user.Id, executeAt);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing escalation reminder for user {UserId}", user.Id);
                    }
                }

                _logger.LogInformation("EscalationMidMorningJob completed. Scheduled {Count} escalation notifications", notificationsSent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EscalationMidMorningJob execution");
                throw;
            }
        }
    }
}
