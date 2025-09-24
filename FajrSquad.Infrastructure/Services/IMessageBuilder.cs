using FajrSquad.Core.Entities;

namespace FajrSquad.Infrastructure.Services
{
    public interface IMessageBuilder
    {
        Task<NotificationRequest> BuildMorningReminderAsync(User user, DeviceToken deviceToken);
        Task<NotificationRequest> BuildEveningReminderAsync(User user, DeviceToken deviceToken);
        Task<NotificationRequest> BuildFajrLateMotivationAsync(User user, DeviceToken deviceToken, TimeSpan fajrTime);
        Task<NotificationRequest> BuildEscalationReminderAsync(User user, DeviceToken deviceToken);
        Task<NotificationRequest> BuildAdminAlertAsync(User user, int consecutiveMissedDays);
        Task<NotificationRequest> BuildDailyHadithAsync(Hadith hadith, User user, DeviceToken deviceToken);
        Task<NotificationRequest> BuildDailyMotivationAsync(Motivation motivation, User user, DeviceToken deviceToken);
        Task<NotificationRequest> BuildEventCreatedAsync(Event eventEntity, User user, DeviceToken deviceToken);
        Task<NotificationRequest> BuildEventReminderAsync(Event eventEntity, User user, DeviceToken deviceToken, string timeUntil);
    }

    public class MessageTemplate
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public Dictionary<string, string> Data { get; set; } = new();
        public string? CollapseKey { get; set; }
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        public int TtlSeconds { get; set; } = 7200;
    }
}
