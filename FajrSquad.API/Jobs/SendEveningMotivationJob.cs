using FajrSquad.Infrastructure.Services;
using Quartz;

namespace FajrSquad.API.Jobs
{
    public class SendEveningMotivationJob : IJob
    {
        private readonly NotificationService _notificationService;

        public SendEveningMotivationJob(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await _notificationService.SendMotivationNotification("night");
        }
    }
}
