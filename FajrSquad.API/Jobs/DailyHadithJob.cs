using FajrSquad.Infrastructure.Services;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace FajrSquad.API.Jobs
{
    [DisallowConcurrentExecution]
    public class DailyHadithJob : IJob
    {
        private readonly INotificationSender _notificationSender;
        private readonly IMessageBuilder _messageBuilder;
        private readonly INotificationScheduler _notificationScheduler;
        private readonly FajrDbContext _db;
        private readonly ILogger<DailyHadithJob> _logger;

        public DailyHadithJob(
            INotificationSender notificationSender,
            IMessageBuilder messageBuilder,
            INotificationScheduler notificationScheduler,
            FajrDbContext db,
            ILogger<DailyHadithJob> logger)
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
                _logger.LogInformation("Starting DailyHadithJob execution");

                var today = DateTime.UtcNow.Date;
                var notificationsSent = 0;

                // Get a random hadith for each language
                var languages = new[] { "it", "fr", "en" };
                var hadithsByLanguage = new Dictionary<string, Hadith>();

                foreach (var language in languages)
                {
                    var hadith = await _db.Hadiths
                        .Where(h => h.Language == language && h.IsActive && !h.IsDeleted)
                        .OrderBy(h => h.Priority)
                        .ThenBy(h => Guid.NewGuid())
                        .FirstOrDefaultAsync();

                    if (hadith != null)
                    {
                        hadithsByLanguage[language] = hadith;
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
                        // Check if user has daily hadith notifications enabled
                        var preferences = user.UserNotificationPreferences?.FirstOrDefault();
                        if (preferences != null && !preferences.HadithDaily)
                        {
                            continue;
                        }

                        // Get user's language
                        var userLanguage = user.DeviceTokens?.FirstOrDefault()?.Language ?? "it";
                        
                        // Check if we have a hadith for this language
                        if (!hadithsByLanguage.TryGetValue(userLanguage, out var hadith))
                        {
                            // Fallback to Italian
                            hadithsByLanguage.TryGetValue("it", out hadith);
                        }

                        if (hadith != null)
                        {
                            // Check if user hasn't already received a daily hadith today
                            var todayKey = $"daily_hadith_{user.Id}_{today:yyyyMMdd}";
                            var existingNotification = await _db.ScheduledNotifications
                                .AnyAsync(sn => sn.UniqueKey == todayKey && sn.Status == "Sent");

                            if (!existingNotification)
                            {
                                // Schedule daily hadith for this user
                                var executeAt = DateTimeOffset.UtcNow.AddMinutes(1);
                                
                                await _notificationScheduler.ScheduleNotificationAsync(
                                    user.Id,
                                    "DailyHadith",
                                    executeAt,
                                    new { 
                                        UserId = user.Id,
                                        HadithId = hadith.Id,
                                        Language = userLanguage
                                    },
                                    todayKey
                                );

                                notificationsSent++;
                                _logger.LogInformation("Scheduled daily hadith for user {UserId} in language {Language} at {ExecuteAt}", 
                                    user.Id, userLanguage, executeAt);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing daily hadith for user {UserId}", user.Id);
                    }
                }

                _logger.LogInformation("DailyHadithJob completed. Scheduled {Count} daily hadith notifications", notificationsSent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DailyHadithJob execution");
                throw;
            }
        }
    }
}
