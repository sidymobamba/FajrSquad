using FirebaseAdmin.Messaging;
using FajrSquad.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace FajrSquad.Infrastructure.Services
{
    public class FcmNotificationSender : INotificationSender
    {
        private readonly FajrDbContext _db;
        private readonly ILogger<FcmNotificationSender> _logger;
        private readonly INotificationMetricsService _metricsService;
        private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

        public FcmNotificationSender(
            FajrDbContext db, 
            ILogger<FcmNotificationSender> logger,
            INotificationMetricsService metricsService)
        {
            _db = db;
            _logger = logger;
            _metricsService = metricsService;
            
            // Configure retry policy with exponential backoff
            _retryPolicy = Policy
                .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .Or<HttpRequestException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning("FCM retry {RetryCount} after {Delay}ms. Reason: {Reason}", 
                            retryCount, timespan.TotalMilliseconds, outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                    });
        }

        public async Task<NotificationResult> SendAsync(NotificationRequest request)
        {
            try
            {
                var message = BuildFirebaseMessage(request);
                var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                
                _logger.LogInformation("FCM notification sent successfully. MessageId: {MessageId}", response);
                
                return new NotificationResult
                {
                    Success = true,
                    MessageId = response,
                    SentAt = DateTimeOffset.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send FCM notification");
                return new NotificationResult
                {
                    Success = false,
                    Error = ex.Message,
                    SentAt = DateTimeOffset.UtcNow
                };
            }
        }

        public async Task<NotificationResult> SendToUserAsync(Guid userId, NotificationRequest request)
        {
            var deviceTokens = await _db.DeviceTokens
                .Where(dt => dt.UserId == userId && dt.IsActive && !dt.IsDeleted)
                .ToListAsync();

            if (!deviceTokens.Any())
            {
                _logger.LogWarning("No active device tokens found for user {UserId}", userId);
                return new NotificationResult
                {
                    Success = false,
                    Error = "No active device tokens found",
                    SentAt = DateTimeOffset.UtcNow
                };
            }

            var results = new List<NotificationResult>();
            var failedTokens = new List<string>();

            foreach (var deviceToken in deviceTokens)
            {
                try
                {
                    var message = BuildFirebaseMessage(request, deviceToken.Token);
                    var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                    
                    results.Add(new NotificationResult
                    {
                        Success = true,
                        MessageId = response,
                        SentAt = DateTimeOffset.UtcNow
                    });
                    
                    _logger.LogInformation("FCM notification sent to user {UserId}, token {TokenId}. MessageId: {MessageId}", 
                        userId, deviceToken.Id, response);
                }
                catch (FirebaseMessagingException ex) when (ex.MessagingErrorCode == MessagingErrorCode.Unregistered)
                {
                    _logger.LogWarning("Device token {TokenId} is unregistered, marking as inactive", deviceToken.Id);
                    deviceToken.IsActive = false;
                    deviceToken.UpdatedAt = DateTimeOffset.UtcNow;
                    failedTokens.Add(deviceToken.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send FCM notification to user {UserId}, token {TokenId}", userId, deviceToken.Id);
                    results.Add(new NotificationResult
                    {
                        Success = false,
                        Error = ex.Message,
                        SentAt = DateTimeOffset.UtcNow
                    });
                }
            }

            // Update failed tokens
            if (failedTokens.Any())
            {
                await _db.SaveChangesAsync();
            }

            // Return success if at least one notification was sent successfully
            var successCount = results.Count(r => r.Success);
            var finalResult = new NotificationResult
            {
                Success = successCount > 0,
                MessageId = successCount > 0 ? $"Batch: {successCount}/{results.Count}" : null,
                Error = successCount == 0 ? "All notifications failed" : null,
                SentAt = DateTimeOffset.UtcNow
            };

            // Log the notification
            await _metricsService.LogNotificationAsync(new Core.Entities.NotificationLog
            {
                UserId = userId,
                Type = "UserNotification",
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(request),
                Result = finalResult.Success ? "Sent" : "Failed",
                ProviderMessageId = finalResult.MessageId,
                Error = finalResult.Error,
                SentAt = finalResult.SentAt,
                Retried = results.Sum(r => r.RetryCount)
            });

            return finalResult;
        }

        public async Task<NotificationResult> SendToTopicAsync(string topic, NotificationRequest request)
        {
            try
            {
                var message = BuildFirebaseMessage(request, topic: topic);
                var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                
                _logger.LogInformation("FCM notification sent to topic {Topic}. MessageId: {MessageId}", topic, response);
                
                return new NotificationResult
                {
                    Success = true,
                    MessageId = response,
                    SentAt = DateTimeOffset.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send FCM notification to topic {Topic}", topic);
                return new NotificationResult
                {
                    Success = false,
                    Error = ex.Message,
                    SentAt = DateTimeOffset.UtcNow
                };
            }
        }

        public async Task<NotificationResult> SendToMultipleUsersAsync(IEnumerable<Guid> userIds, NotificationRequest request)
        {
            var results = new List<NotificationResult>();
            
            foreach (var userId in userIds)
            {
                var result = await SendToUserAsync(userId, request);
                results.Add(result);
            }

            var successCount = results.Count(r => r.Success);
            return new NotificationResult
            {
                Success = successCount > 0,
                MessageId = successCount > 0 ? $"Batch: {successCount}/{results.Count}" : null,
                Error = successCount == 0 ? "All notifications failed" : null,
                SentAt = DateTimeOffset.UtcNow
            };
        }

        private Message BuildFirebaseMessage(NotificationRequest request, string? token = null, string? topic = null)
        {
            var message = new Message
            {
                Notification = new Notification
                {
                    Title = request.Title,
                    Body = request.Body,
                    ImageUrl = request.ImageUrl
                },
                Data = request.Data,
                Android = new AndroidConfig
                {
                    Priority = request.Priority switch
                    {
                        NotificationPriority.High => Priority.High,
                        NotificationPriority.Low => Priority.Normal,
                        _ => Priority.Normal
                    },
                    Notification = new AndroidNotification
                    {
                        ChannelId = GetChannelId(request),
                        Sound = request.Sound ?? "default"
                    }
                },
                Apns = new ApnsConfig
                {
                    Headers = new Dictionary<string, string>
                    {
                        ["apns-push-type"] = request.ContentAvailable ? "background" : "alert",
                        ["apns-collapse-id"] = request.CollapseKey ?? string.Empty
                    },
                    Aps = new Aps
                    {
                        ContentAvailable = request.ContentAvailable,
                        Sound = request.Sound ?? "default",
                        Alert = new ApsAlert
                        {
                            Title = request.Title,
                            Body = request.Body
                        }
                    }
                }
            };

            if (!string.IsNullOrEmpty(token))
            {
                message.Token = token;
            }
            else if (!string.IsNullOrEmpty(topic))
            {
                message.Topic = topic;
            }

            return message;
        }

        private string GetChannelId(NotificationRequest request)
        {
            // Determine channel based on notification type or priority
            return request.Priority switch
            {
                NotificationPriority.High => "fajr_urgent",
                NotificationPriority.Low => "general",
                _ => "fajr_reminders"
            };
        }
    }
}
