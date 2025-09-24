using FajrSquad.Core.Entities;

namespace FajrSquad.Infrastructure.Services
{
    public interface INotificationMetricsService
    {
        Task LogNotificationAsync(NotificationLog log);
        Task<NotificationMetrics> GetMetricsAsync(DateTimeOffset from, DateTimeOffset to);
        Task<NotificationMetrics> GetUserMetricsAsync(Guid userId, DateTimeOffset from, DateTimeOffset to);
        Task CleanupOldLogsAsync(int daysToKeep = 30);
    }

    public class NotificationMetrics
    {
        public int TotalSent { get; set; }
        public int TotalFailed { get; set; }
        public int TotalRetried { get; set; }
        public Dictionary<string, int> SentByType { get; set; } = new();
        public Dictionary<string, int> FailedByType { get; set; } = new();
        public Dictionary<string, int> SentByHour { get; set; } = new();
        public double SuccessRate { get; set; }
        public TimeSpan AverageProcessingTime { get; set; }
        public List<string> TopErrors { get; set; } = new();
    }
}
