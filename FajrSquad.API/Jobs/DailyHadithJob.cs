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
    public class DailyHadithJob : IJob
    {
        private readonly FajrDbContext _db;
        private readonly INotificationScheduler _scheduler;
        private readonly ILogger<DailyHadithJob> _logger;
        private readonly IConfiguration _configuration;

        public DailyHadithJob(
            FajrDbContext db,
            INotificationScheduler scheduler,
            ILogger<DailyHadithJob> logger,
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
                _logger.LogInformation("Starting DailyHadithJob execution");

                var users = await _db.Users
                    .Include(u => u.UserNotificationPreferences)
                    .Include(u => u.DeviceTokens.Where(dt => dt.IsActive && !dt.IsDeleted))
                    .Where(u => !u.IsDeleted)
                    .ToListAsync();

                var hadiths = await _db.Hadiths
                    .Where(h => h.IsActive)
                    .OrderBy(h => h.Priority)
                    .ToListAsync();

                if (!hadiths.Any())
                {
                    _logger.LogWarning("No active hadiths found for daily notifications");
                    return;
                }

                var random = new Random();
                var notificationsScheduled = 0;

                foreach (var user in users)
                {
                    try
                    {
                        var preferences = user.UserNotificationPreferences?.FirstOrDefault();
                        if (preferences?.HadithDaily != true)
                        {
                            _logger.LogDebug("User {UserId} has daily hadith notifications disabled", user.Id);
                            continue;
                        }

                        if (!user.DeviceTokens.Any())
                        {
                            _logger.LogDebug("User {UserId} has no active device tokens", user.Id);
                            continue;
                        }

                        // Check if user already received a hadith today
                        var today = DateTimeOffset.UtcNow.Date;
                        var alreadyReceived = await _db.NotificationLogs
                            .AnyAsync(nl => nl.UserId == user.Id && 
                                          nl.Type == "DailyHadith" && 
                                          nl.CreatedAt.Date == today);

                        if (alreadyReceived)
                        {
                            _logger.LogDebug("User {UserId} already received daily hadith today", user.Id);
                            continue;
                        }

                        // Select a random hadith
                        var selectedHadith = hadiths[random.Next(hadiths.Count)];

                        // Schedule notification for immediate delivery (or within next few minutes)
                        var executeAt = DateTimeOffset.UtcNow.AddMinutes(1);

                        var notification = new ScheduledNotification
                        {
                            UserId = user.Id,
                            Type = "DailyHadith",
                            ExecuteAt = executeAt,
                            DataJson = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                UserId = user.Id,
                                HadithId = selectedHadith.Id
                            }),
                            UniqueKey = $"daily_hadith:{user.Id}:{today:yyyyMMdd}",
                            MaxRetries = 3
                        };

                        await _scheduler.ScheduleNotificationAsync(notification.UserId, notification.Type, notification.ExecuteAt, 
                            System.Text.Json.JsonSerializer.Deserialize<object>(notification.DataJson), notification.UniqueKey);
                        notificationsScheduled++;

                        _logger.LogInformation("Scheduled daily hadith notification for user {UserId} with hadith {HadithId}", 
                            user.Id, selectedHadith.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing daily hadith for user {UserId}", user.Id);
                    }
                }

                _logger.LogInformation("DailyHadithJob completed. Scheduled {Count} notifications", notificationsScheduled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DailyHadithJob execution");
                throw;
            }
        }
    }
}