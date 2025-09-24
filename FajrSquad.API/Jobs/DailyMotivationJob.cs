using FajrSquad.Infrastructure.Services;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace FajrSquad.API.Jobs
{
    [DisallowConcurrentExecution]
    public class DailyMotivationJob : IJob
    {
        private readonly INotificationSender _notificationSender;
        private readonly IMessageBuilder _messageBuilder;
        private readonly INotificationScheduler _notificationScheduler;
        private readonly FajrDbContext _db;
        private readonly ILogger<DailyMotivationJob> _logger;

        public DailyMotivationJob(
            INotificationSender notificationSender,
            IMessageBuilder messageBuilder,
            INotificationScheduler notificationScheduler,
            FajrDbContext db,
            ILogger<DailyMotivationJob> logger)
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
                _logger.LogInformation("Starting DailyMotivationJob execution");

                var today = DateTime.UtcNow.Date;
                var notificationsSent = 0;

                // Get a random motivation for each language
                var languages = new[] { "it", "fr", "en" };
                var motivationsByLanguage = new Dictionary<string, Motivation>();

                foreach (var language in languages)
                {
                    var motivation = await _db.Motivations
                        .Where(m => m.Language == language && m.IsActive && !m.IsDeleted)
                        .OrderBy(m => m.Priority)
                        .ThenBy(m => Guid.NewGuid())
                        .FirstOrDefaultAsync();

                    if (motivation != null)
                    {
                        motivationsByLanguage[language] = motivation;
                    }
                }

                // Get all active users with their device tokens and preferences
                var users = await _db.Users
                    .Where(u => !u.IsDeleted)
                    .Include(u => u.DeviceTokens.Where(dt => dt.IsActive && !dt.IsDeleted))
                    .Include(u => u.UserNotificationPreferences)
                    .ToListAsync();

                foreach (var user in users)
                {
                    try
                    {
                        // Check if user has daily motivation notifications enabled
                        var preferences = user.UserNotificationPreferences?.FirstOrDefault();
                        if (preferences != null && !preferences.MotivationDaily)
                        {
                            continue;
                        }

                        // Get user's language
                        var userLanguage = user.DeviceTokens?.FirstOrDefault()?.Language ?? "it";
                        
                        // Check if we have a motivation for this language
                        if (!motivationsByLanguage.TryGetValue(userLanguage, out var motivation))
                        {
                            // Fallback to Italian
                            motivationsByLanguage.TryGetValue("it", out motivation);
                        }

                        if (motivation != null)
                        {
                            // Check if user hasn't already received a daily motivation today
                            var todayKey = $"daily_motivation_{user.Id}_{today:yyyyMMdd}";
                            var existingNotification = await _db.ScheduledNotifications
                                .AnyAsync(sn => sn.UniqueKey == todayKey && sn.Status == "Sent");

                            if (!existingNotification)
                            {
                                // Schedule daily motivation for this user
                                var executeAt = DateTimeOffset.UtcNow.AddMinutes(1);
                                
                                await _notificationScheduler.ScheduleNotificationAsync(
                                    user.Id,
                                    "DailyMotivation",
                                    executeAt,
                                    new { 
                                        UserId = user.Id,
                                        MotivationId = motivation.Id,
                                        Language = userLanguage
                                    },
                                    todayKey
                                );

                                notificationsSent++;
                                _logger.LogInformation("Scheduled daily motivation for user {UserId} in language {Language} at {ExecuteAt}", 
                                    user.Id, userLanguage, executeAt);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing daily motivation for user {UserId}", user.Id);
                    }
                }

                _logger.LogInformation("DailyMotivationJob completed. Scheduled {Count} daily motivation notifications", notificationsSent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DailyMotivationJob execution");
                throw;
            }
        }
    }
}
