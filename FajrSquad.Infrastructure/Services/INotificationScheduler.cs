using FajrSquad.Core.Entities;

namespace FajrSquad.Infrastructure.Services
{
    public interface INotificationScheduler
    {
        Task ScheduleNotificationAsync(Guid? userId, string type, DateTimeOffset executeAt, object data, string? uniqueKey = null);
        Task ScheduleEventRemindersAsync(Event eventEntity);
        Task ProcessScheduledNotificationsAsync();
        Task CancelScheduledNotificationAsync(string uniqueKey);
        Task<List<ScheduledNotification>> GetPendingNotificationsAsync(DateTimeOffset? before = null);
    }
}
