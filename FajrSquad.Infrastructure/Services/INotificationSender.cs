using FajrSquad.Core.Entities;

namespace FajrSquad.Infrastructure.Services
{
    public interface INotificationSender
    {
        Task<NotificationResult> SendAsync(NotificationRequest request);
        Task<NotificationResult> SendToUserAsync(Guid userId, NotificationRequest request);
        Task<NotificationResult> SendToTopicAsync(string topic, NotificationRequest request);
        Task<NotificationResult> SendToMultipleUsersAsync(IEnumerable<Guid> userIds, NotificationRequest request);
    }

    public class NotificationRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public Dictionary<string, string> Data { get; set; } = new();
        public string? CollapseKey { get; set; }
        public int? TtlSeconds { get; set; }
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        public string? Sound { get; set; }
        public bool ContentAvailable { get; set; } = false;
    }

    public class NotificationResult
    {
        public bool Success { get; set; }
        public string? MessageId { get; set; }
        public string? Error { get; set; }
        public int RetryCount { get; set; }
        public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
    }

    public enum NotificationPriority
    {
        Low,
        Normal,
        High
    }
}
