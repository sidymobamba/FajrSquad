using FajrSquad.Infrastructure.Services;
using Quartz;

namespace FajrSquad.API.Jobs
{
    public class SendMorningMotivationJob : IJob
    {
        private readonly NotificationService _notificationService;

        public SendMorningMotivationJob(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await _notificationService.SendMotivationNotification("fajr");
        }
    }

}
