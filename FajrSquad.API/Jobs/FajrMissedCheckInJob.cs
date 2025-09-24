using FajrSquad.Core.Entities;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

namespace FajrSquad.API.Jobs
{
    [DisallowConcurrentExecution]
    public class FajrMissedCheckInJob : IJob
    {
        private readonly FajrDbContext _db;
        private readonly INotificationScheduler _scheduler;
        private readonly ILogger<FajrMissedCheckInJob> _logger;
        private readonly IConfiguration _configuration;

        public FajrMissedCheckInJob(
            FajrDbContext db,
            INotificationScheduler scheduler,
            ILogger<FajrMissedCheckInJob> logger,
            IConfiguration configuration)
        {
            _db = db;
            _scheduler = scheduler;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Starting FajrMissedCheckInJob execution");

                var today = DateTimeOffset.UtcNow.Date;
                var fajrTime = TimeSpan.Parse("05:30"); // TODO: Get actual Fajr time based on location
                var lateMorningTime = TimeSpan.Parse("10:00"); // Late morning motivation
                var midMorningTime = TimeSpan.Parse("08:30"); // Escalation reminder

                // Get users who should have checked in for Fajr today
                var users = await _db.Users
                    .Include(u => u.UserNotificationPreferences)
                    .Include(u => u.DeviceTokens.Where(dt => dt.IsActive && !dt.IsDeleted))
                    .Include(u => u.FajrCheckIns.Where(f => f.Date.Date == today))
                    .Where(u => !u.IsDeleted)
                    .ToListAsync();

                var lateMotivationsScheduled = 0;
                var escalationRemindersScheduled = 0;
                var adminAlertsScheduled = 0;

                foreach (var user in users)
                {
                    try
                    {
                        var preferences = user.UserNotificationPreferences?.FirstOrDefault();
                        if (preferences == null)
                        {
                            _logger.LogDebug("User {UserId} has no notification preferences", user.Id);
                            continue;
                        }

                        if (!user.DeviceTokens.Any())
                        {
                            _logger.LogDebug("User {UserId} has no active device tokens", user.Id);
                            continue;
                        }

                        var hasCheckedInToday = user.FajrCheckIns.Any(f => f.Date.Date == today);

                        if (hasCheckedInToday)
                        {
                            _logger.LogDebug("User {UserId} has already checked in for Fajr today", user.Id);
                            continue;
                        }

                        // Check consecutive missed days
                        var consecutiveMissedDays = await GetConsecutiveMissedDaysAsync(user.Id, today);

                        // Schedule late morning motivation (if enabled and not already sent)
                        if (preferences.FajrMissed)
                        {
                            var alreadySentLateMotivation = await _db.NotificationLogs
                                .AnyAsync(nl => nl.UserId == user.Id && 
                                              nl.Type == "FajrLateMotivation" && 
                                              nl.CreatedAt.Date == today);

                            if (!alreadySentLateMotivation)
                            {
                                var lateMotivationExecuteAt = DateTimeOffset.UtcNow.AddMinutes(1);

                                var lateMotivationNotification = new ScheduledNotification
                                {
                                    UserId = user.Id,
                                    Type = "FajrLateMotivation",
                                    ExecuteAt = lateMotivationExecuteAt,
                                    DataJson = System.Text.Json.JsonSerializer.Serialize(new
                                    {
                                        UserId = user.Id,
                                        FajrTime = fajrTime.ToString(@"hh\:mm")
                                    }),
                                    UniqueKey = $"fajr_late_motivation:{user.Id}:{today:yyyyMMdd}",
                                    MaxRetries = 3
                                };

                                await _scheduler.ScheduleNotificationAsync(lateMotivationNotification.UserId, lateMotivationNotification.Type, lateMotivationNotification.ExecuteAt, 
                                    System.Text.Json.JsonSerializer.Deserialize<object>(lateMotivationNotification.DataJson), lateMotivationNotification.UniqueKey);
                                lateMotivationsScheduled++;

                                _logger.LogInformation("Scheduled late morning motivation for user {UserId}", user.Id);
                            }
                        }

                        // Schedule escalation reminder (if enabled and not already sent)
                        if (preferences.Escalation && consecutiveMissedDays >= 2)
                        {
                            var alreadySentEscalation = await _db.NotificationLogs
                                .AnyAsync(nl => nl.UserId == user.Id && 
                                              nl.Type == "EscalationReminder" && 
                                              nl.CreatedAt.Date == today);

                            if (!alreadySentEscalation)
                            {
                                var escalationExecuteAt = DateTimeOffset.UtcNow.AddMinutes(2);

                                var escalationNotification = new ScheduledNotification
                                {
                                    UserId = user.Id,
                                    Type = "EscalationReminder",
                                    ExecuteAt = escalationExecuteAt,
                                    DataJson = System.Text.Json.JsonSerializer.Serialize(new
                                    {
                                        UserId = user.Id,
                                        ConsecutiveDays = consecutiveMissedDays
                                    }),
                                    UniqueKey = $"escalation_reminder:{user.Id}:{today:yyyyMMdd}",
                                    MaxRetries = 3
                                };

                                await _scheduler.ScheduleNotificationAsync(escalationNotification.UserId, escalationNotification.Type, escalationNotification.ExecuteAt, 
                                    System.Text.Json.JsonSerializer.Deserialize<object>(escalationNotification.DataJson), escalationNotification.UniqueKey);
                                escalationRemindersScheduled++;

                                _logger.LogInformation("Scheduled escalation reminder for user {UserId} (consecutive days: {Days})", 
                                    user.Id, consecutiveMissedDays);
                            }
                        }

                        // Schedule admin alert (if 3+ consecutive days)
                        if (consecutiveMissedDays >= 3)
                        {
                            var alreadySentAdminAlert = await _db.NotificationLogs
                                .AnyAsync(nl => nl.UserId == user.Id && 
                                              nl.Type == "AdminAlert" && 
                                              nl.CreatedAt.Date == today);

                            if (!alreadySentAdminAlert)
                            {
                                var adminAlertExecuteAt = DateTimeOffset.UtcNow.AddMinutes(3);

                                var adminAlertNotification = new ScheduledNotification
                                {
                                    UserId = user.Id,
                                    Type = "AdminAlert",
                                    ExecuteAt = adminAlertExecuteAt,
                                    DataJson = System.Text.Json.JsonSerializer.Serialize(new
                                    {
                                        UserId = user.Id,
                                        ConsecutiveMissedDays = consecutiveMissedDays
                                    }),
                                    UniqueKey = $"admin_alert:{user.Id}:{today:yyyyMMdd}",
                                    MaxRetries = 3
                                };

                                await _scheduler.ScheduleNotificationAsync(adminAlertNotification.UserId, adminAlertNotification.Type, adminAlertNotification.ExecuteAt, 
                                    System.Text.Json.JsonSerializer.Deserialize<object>(adminAlertNotification.DataJson), adminAlertNotification.UniqueKey);
                                adminAlertsScheduled++;

                                _logger.LogWarning("Scheduled admin alert for user {UserId} (consecutive days: {Days})", 
                                    user.Id, consecutiveMissedDays);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing Fajr missed check-in for user {UserId}", user.Id);
                    }
                }

                _logger.LogInformation("FajrMissedCheckInJob completed. Scheduled {LateMotivations} late motivations, {Escalations} escalations, {AdminAlerts} admin alerts", 
                    lateMotivationsScheduled, escalationRemindersScheduled, adminAlertsScheduled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FajrMissedCheckInJob execution");
                throw;
            }
        }

        private async Task<int> GetConsecutiveMissedDaysAsync(Guid userId, DateTimeOffset today)
        {
            var consecutiveDays = 0;
            var checkDate = today;

            while (consecutiveDays < 7) // Check max 7 days back
            {
                var hasCheckedIn = await _db.FajrCheckIns
                    .AnyAsync(f => f.UserId == userId && f.Date.Date == checkDate.Date);

                if (hasCheckedIn)
                {
                    break;
                }

                consecutiveDays++;
                checkDate = checkDate.AddDays(-1);
            }

            return consecutiveDays;
        }
    }
}
