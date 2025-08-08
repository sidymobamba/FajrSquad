using FajrSquad.Infrastructure.Services;
using Quartz;

namespace FajrSquad.API.Jobs
{
    public class SendAfternoonMotivationJob : IJob
    {
        private readonly NotificationService _notificationService;

        public SendAfternoonMotivationJob(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await _notificationService.SendMotivationNotification("afternoon");
        }
    }
}
