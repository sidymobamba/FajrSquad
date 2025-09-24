using FajrSquad.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace FajrSquad.API.Jobs
{
    [DisallowConcurrentExecution]
    public class NotificationCleanupJob : IJob
    {
        private readonly INotificationMetricsService _metricsService;
        private readonly ILogger<NotificationCleanupJob> _logger;

        public NotificationCleanupJob(
            INotificationMetricsService metricsService,
            ILogger<NotificationCleanupJob> logger)
        {
            _metricsService = metricsService;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Starting NotificationCleanupJob execution");

                // Clean up notification logs older than 30 days
                await _metricsService.CleanupOldLogsAsync(30);

                _logger.LogInformation("NotificationCleanupJob completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NotificationCleanupJob execution");
                throw;
            }
        }
    }
}
