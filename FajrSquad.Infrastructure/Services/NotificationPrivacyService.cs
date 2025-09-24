using FajrSquad.Core.Entities;
using FajrSquad.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace FajrSquad.Infrastructure.Services
{
    public class NotificationPrivacyService : INotificationPrivacyService
    {
        private readonly FajrDbContext _db;
        private readonly ILogger<NotificationPrivacyService> _logger;
        private readonly IConfiguration _configuration;

        // Urgent notification types that bypass quiet hours and rate limits
        private readonly HashSet<string> _urgentNotificationTypes = new()
        {
            "AdminAlert",
            "FajrLateMotivation",
            "EscalationReminder"
        };

        public NotificationPrivacyService(
            FajrDbContext db, 
            ILogger<NotificationPrivacyService> logger,
            IConfiguration configuration)
        {
            _db = db;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<bool> ShouldSendNotificationAsync(Guid userId, string notificationType, DateTimeOffset scheduledTime)
        {
            try
            {
                // Check if notification is urgent (bypasses most restrictions)
                if (await IsNotificationUrgentAsync(notificationType))
                {
                    return true;
                }

                // Check user preferences
                var preferences = await _db.UserNotificationPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (preferences != null)
                {
                    // Check if this type of notification is enabled
                    if (!IsNotificationTypeEnabled(preferences, notificationType))
                    {
                        _logger.LogDebug("Notification {Type} disabled for user {UserId}", notificationType, userId);
                        return false;
                    }
                }

                // Check quiet hours
                if (await IsWithinQuietHoursAsync(userId, scheduledTime))
                {
                    _logger.LogDebug("Notification {Type} blocked due to quiet hours for user {UserId}", notificationType, userId);
                    return false;
                }

                // Check daily rate limit
                if (await HasExceededDailyLimitAsync(userId, notificationType))
                {
                    _logger.LogDebug("Notification {Type} blocked due to daily limit for user {UserId}", notificationType, userId);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking notification privacy for user {UserId}, type {Type}", userId, notificationType);
                // Default to allowing the notification if there's an error
                return true;
            }
        }

        public async Task<bool> IsWithinQuietHoursAsync(Guid userId, DateTimeOffset scheduledTime)
        {
            try
            {
                var preferences = await _db.UserNotificationPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (preferences?.QuietHoursStart == null || preferences.QuietHoursEnd == null)
                {
                    // Use default quiet hours from configuration
                    var defaultStart = TimeSpan.Parse(_configuration["Notifications:BusinessRules:QuietHoursStart"] ?? "22:00");
                    var defaultEnd = TimeSpan.Parse(_configuration["Notifications:BusinessRules:QuietHoursEnd"] ?? "06:00");
                    
                    return IsTimeWithinQuietHours(scheduledTime, defaultStart, defaultEnd);
                }

                return IsTimeWithinQuietHours(scheduledTime, preferences.QuietHoursStart.Value, preferences.QuietHoursEnd.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking quiet hours for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> HasExceededDailyLimitAsync(Guid userId, string notificationType)
        {
            try
            {
                var today = DateTimeOffset.UtcNow.Date;
                var dailyCount = await GetDailyNotificationCountAsync(userId, today);
                
                var maxDaily = int.Parse(_configuration["Notifications:BusinessRules:MaxNotificationsPerDay"] ?? "10");
                
                return dailyCount >= maxDaily;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking daily limit for user {UserId}", userId);
                return false;
            }
        }

        public async Task<int> GetDailyNotificationCountAsync(Guid userId, DateTimeOffset date)
        {
            try
            {
                var startOfDay = date.Date;
                var endOfDay = startOfDay.AddDays(1);

                return await _db.NotificationLogs
                    .Where(nl => nl.UserId == userId && 
                                nl.SentAt >= startOfDay && 
                                nl.SentAt < endOfDay &&
                                nl.Result == "Sent")
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily notification count for user {UserId}", userId);
                return 0;
            }
        }

        public Task<bool> IsNotificationUrgentAsync(string notificationType)
        {
            return Task.FromResult(_urgentNotificationTypes.Contains(notificationType));
        }

        private bool IsTimeWithinQuietHours(DateTimeOffset scheduledTime, TimeSpan quietStart, TimeSpan quietEnd)
        {
            var localTime = scheduledTime.TimeOfDay;

            if (quietStart <= quietEnd)
            {
                // Same day quiet hours (e.g., 22:00 to 06:00 next day)
                return localTime >= quietStart && localTime <= quietEnd;
            }
            else
            {
                // Overnight quiet hours (e.g., 22:00 to 06:00 next day)
                return localTime >= quietStart || localTime <= quietEnd;
            }
        }

        private bool IsNotificationTypeEnabled(UserNotificationPreference preferences, string notificationType)
        {
            return notificationType switch
            {
                "MorningReminder" => preferences.Morning,
                "EveningReminder" => preferences.Evening,
                "FajrLateMotivation" => preferences.FajrMissed,
                "EscalationReminder" => preferences.Escalation,
                "DailyHadith" => preferences.HadithDaily,
                "DailyMotivation" => preferences.MotivationDaily,
                "EventCreated" => preferences.EventsNew,
                "EventReminder" => preferences.EventsReminder,
                _ => true // Default to enabled for unknown types
            };
        }
    }
}
