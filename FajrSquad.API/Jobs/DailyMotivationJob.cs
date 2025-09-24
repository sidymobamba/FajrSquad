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
    public class DailyMotivationJob : IJob
    {
        private readonly FajrDbContext _db;
        private readonly INotificationScheduler _scheduler;
        private readonly ILogger<DailyMotivationJob> _logger;
        private readonly IConfiguration _configuration;

        public DailyMotivationJob(
            FajrDbContext db,
            INotificationScheduler scheduler,
            ILogger<DailyMotivationJob> logger,
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
                _logger.LogInformation("Starting DailyMotivationJob execution");

                var users = await _db.Users
                    .Include(u => u.UserNotificationPreferences)
                    .Include(u => u.DeviceTokens.Where(dt => dt.IsActive && !dt.IsDeleted))
                    .Where(u => !u.IsDeleted)
                    .ToListAsync();

                var motivations = await _db.Motivations
                    .Where(m => m.IsActive)
                    .OrderBy(m => m.Priority)
                    .ToListAsync();

                if (!motivations.Any())
                {
                    _logger.LogWarning("No active motivations found for daily notifications");
                    return;
                }

                var random = new Random();
                var notificationsScheduled = 0;

                foreach (var user in users)
                {
                    try
                    {
                        var preferences = user.UserNotificationPreferences?.FirstOrDefault();
                        if (preferences?.MotivationDaily != true)
                        {
                            _logger.LogDebug("User {UserId} has daily motivation notifications disabled", user.Id);
                            continue;
                        }

                        if (!user.DeviceTokens.Any())
                        {
                            _logger.LogDebug("User {UserId} has no active device tokens", user.Id);
                            continue;
                        }

                        // Check if user already received a motivation today
                        var today = DateTimeOffset.UtcNow.Date;
                        var alreadyReceived = await _db.NotificationLogs
                            .AnyAsync(nl => nl.UserId == user.Id && 
                                          nl.Type == "DailyMotivation" && 
                                          nl.CreatedAt.Date == today);

                        if (alreadyReceived)
                        {
                            _logger.LogDebug("User {UserId} already received daily motivation today", user.Id);
                            continue;
                        }

                        // Select a random motivation
                        var selectedMotivation = motivations[random.Next(motivations.Count)];

                        // Schedule notification for immediate delivery (or within next few minutes)
                        var executeAt = DateTimeOffset.UtcNow.AddMinutes(1);

                        var notification = new ScheduledNotification
                        {
                            UserId = user.Id,
                            Type = "DailyMotivation",
                            ExecuteAt = executeAt,
                            DataJson = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                UserId = user.Id,
                                MotivationId = selectedMotivation.Id
                            }),
                            UniqueKey = $"daily_motivation:{user.Id}:{today:yyyyMMdd}",
                            MaxRetries = 3
                        };

                        await _scheduler.ScheduleNotificationAsync(notification.UserId, notification.Type, notification.ExecuteAt, 
                            System.Text.Json.JsonSerializer.Deserialize<object>(notification.DataJson), notification.UniqueKey);
                        notificationsScheduled++;

                        _logger.LogInformation("Scheduled daily motivation notification for user {UserId} with motivation {MotivationId}", 
                            user.Id, selectedMotivation.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing daily motivation for user {UserId}", user.Id);
                    }
                }

                _logger.LogInformation("DailyMotivationJob completed. Scheduled {Count} notifications", notificationsScheduled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DailyMotivationJob execution");
                throw;
            }
        }
    }
}