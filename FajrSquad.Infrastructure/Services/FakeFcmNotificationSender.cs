using FajrSquad.Infrastructure.Data;
using FajrSquad.Infrastructure.Services;
using FajrSquad.Core.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace FajrSquad.Infrastructure.Services
{
    public class FakeFcmNotificationSender : INotificationSender
    {
        private readonly FajrDbContext _db;
        private readonly ILogger<FakeFcmNotificationSender> _logger;
        private readonly INotificationMetricsService _metricsService;
        private readonly ConcurrentBag<FakeNotification> _sentNotifications;

        public FakeFcmNotificationSender(
            FajrDbContext db, 
            ILogger<FakeFcmNotificationSender> logger,
            INotificationMetricsService metricsService)
        {
            _db = db;
            _logger = logger;
            _metricsService = metricsService;
            _sentNotifications = new ConcurrentBag<FakeNotification>();
        }

        public async Task<NotificationResult> SendAsync(NotificationRequest request)
        {
            var messageId = Guid.NewGuid().ToString();
            
            _logger.LogInformation("FAKE FCM: Sending notification with messageId {MessageId}", messageId);

            var fakeNotification = new FakeNotification
            {
                MessageId = messageId,
                Token = "FAKE_TOKEN",
                Title = request.Title,
                Body = request.Body,
                Data = request.Data,
                SentAt = DateTimeOffset.UtcNow
            };

            _sentNotifications.Add(fakeNotification);

            // Log the notification
            var log = new NotificationLog
            {
                UserId = null,
                Type = "Unknown",
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { title = request.Title, body = request.Body, data = request.Data }),
                Result = "Sent",
                ProviderMessageId = messageId,
                Error = null,
                CollapsibleKey = null,
                SentAt = DateTimeOffset.UtcNow,
                Retried = 0
            };
            await _metricsService.LogNotificationAsync(log);

            _logger.LogInformation("FAKE FCM: Notification sent successfully with messageId {MessageId}", messageId);

            return new NotificationResult
            {
                Success = true,
                MessageId = messageId,
                SentAt = DateTimeOffset.UtcNow
            };
        }

        public async Task<NotificationResult> SendToUserAsync(Guid userId, NotificationRequest request)
        {
            var deviceToken = await _db.DeviceTokens
                .FirstOrDefaultAsync(dt => dt.UserId == userId && dt.IsActive);

            if (deviceToken == null)
            {
                _logger.LogWarning("FAKE FCM: No active device token found for user {UserId}", userId);
                return new NotificationResult
                {
                    Success = false,
                    Error = "No active device token found",
                    SentAt = DateTimeOffset.UtcNow
                };
            }

            var messageId = Guid.NewGuid().ToString();
            
            _logger.LogInformation("FAKE FCM: Sending notification to user {UserId} with messageId {MessageId}", 
                userId, messageId);

            var fakeNotification = new FakeNotification
            {
                MessageId = messageId,
                Token = deviceToken.Token,
                Title = request.Title,
                Body = request.Body,
                Data = request.Data,
                SentAt = DateTimeOffset.UtcNow
            };

            _sentNotifications.Add(fakeNotification);

            // Log the notification
            var log = new NotificationLog
            {
                UserId = userId,
                Type = "Unknown",
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { userId, title = request.Title, body = request.Body, data = request.Data }),
                Result = "Sent",
                ProviderMessageId = messageId,
                Error = null,
                CollapsibleKey = null,
                SentAt = DateTimeOffset.UtcNow,
                Retried = 0
            };
            await _metricsService.LogNotificationAsync(log);

            _logger.LogInformation("FAKE FCM: Notification sent successfully with messageId {MessageId}", messageId);

            return new NotificationResult
            {
                Success = true,
                MessageId = messageId,
                SentAt = DateTimeOffset.UtcNow
            };
        }

        public async Task<NotificationResult> SendToTopicAsync(string topic, NotificationRequest request)
        {
            var messageId = Guid.NewGuid().ToString();
            
            _logger.LogInformation("FAKE FCM: Sending notification to topic {Topic} with messageId {MessageId}", 
                topic, messageId);

            var fakeNotification = new FakeNotification
            {
                MessageId = messageId,
                Token = $"TOPIC:{topic}",
                Title = request.Title,
                Body = request.Body,
                Data = request.Data,
                SentAt = DateTimeOffset.UtcNow
            };

            _sentNotifications.Add(fakeNotification);

            // Log the notification
            var log = new NotificationLog
            {
                UserId = null,
                Type = "Topic",
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { topic, title = request.Title, body = request.Body, data = request.Data }),
                Result = "Sent",
                ProviderMessageId = messageId,
                Error = null,
                CollapsibleKey = null,
                SentAt = DateTimeOffset.UtcNow,
                Retried = 0
            };
            await _metricsService.LogNotificationAsync(log);

            _logger.LogInformation("FAKE FCM: Topic notification sent successfully with messageId {MessageId}", messageId);

            return new NotificationResult
            {
                Success = true,
                MessageId = messageId,
                SentAt = DateTimeOffset.UtcNow
            };
        }

        public async Task<NotificationResult> SendToMultipleUsersAsync(IEnumerable<Guid> userIds, NotificationRequest request)
        {
            var messageId = Guid.NewGuid().ToString();
            var successCount = 0;
            var errorCount = 0;
            
            _logger.LogInformation("FAKE FCM: Sending notification to {Count} users with messageId {MessageId}", 
                userIds.Count(), messageId);

            foreach (var userId in userIds)
            {
                var deviceToken = await _db.DeviceTokens
                    .FirstOrDefaultAsync(dt => dt.UserId == userId && dt.IsActive);

                if (deviceToken != null)
                {
                    var fakeNotification = new FakeNotification
                    {
                        MessageId = $"{messageId}_{userId}",
                        Token = deviceToken.Token,
                        Title = request.Title,
                        Body = request.Body,
                        Data = request.Data,
                        SentAt = DateTimeOffset.UtcNow
                    };

                    _sentNotifications.Add(fakeNotification);
                    successCount++;
                }
                else
                {
                    errorCount++;
                }
            }

            // Log the notification
            var log = new NotificationLog
            {
                UserId = null,
                Type = "MultipleUsers",
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { userIds = userIds.ToArray(), title = request.Title, body = request.Body, data = request.Data }),
                Result = "Sent",
                ProviderMessageId = messageId,
                Error = null,
                CollapsibleKey = null,
                SentAt = DateTimeOffset.UtcNow,
                Retried = 0
            };
            await _metricsService.LogNotificationAsync(log);

            _logger.LogInformation("FAKE FCM: Multiple users notification sent successfully. Success: {Success}, Errors: {Errors}", 
                successCount, errorCount);

            return new NotificationResult
            {
                Success = successCount > 0,
                MessageId = messageId,
                Error = errorCount > 0 ? $"{errorCount} users had no active tokens" : null,
                SentAt = DateTimeOffset.UtcNow
            };
        }

        // Helper method to get sent notifications for testing
        public IReadOnlyList<FakeNotification> GetSentNotifications()
        {
            return _sentNotifications.ToList();
        }

        // Helper method to clear sent notifications for testing
        public void ClearSentNotifications()
        {
            while (_sentNotifications.TryTake(out _)) { }
        }
    }

    public class FakeNotification
    {
        public string MessageId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public Dictionary<string, string>? Data { get; set; }
        public DateTimeOffset SentAt { get; set; }
    }
}
