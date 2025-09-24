using FajrSquad.Core.Entities;

namespace FajrSquad.Infrastructure.Services
{
    public interface INotificationPrivacyService
    {
        Task<bool> ShouldSendNotificationAsync(Guid userId, string notificationType, DateTimeOffset scheduledTime);
        Task<bool> IsWithinQuietHoursAsync(Guid userId, DateTimeOffset scheduledTime);
        Task<bool> HasExceededDailyLimitAsync(Guid userId, string notificationType);
        Task<int> GetDailyNotificationCountAsync(Guid userId, DateTimeOffset date);
        Task<bool> IsNotificationUrgentAsync(string notificationType);
    }
}
