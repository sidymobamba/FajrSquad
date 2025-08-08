using FajrSquad.Infrastructure.Services;
using Quartz;

namespace FajrSquad.API.Jobs
{
    public class SendHadithJob : IJob
    {
        private readonly NotificationService _notificationService;

        public SendHadithJob(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await _notificationService.SendHadithNotification();
        }
    }
}
