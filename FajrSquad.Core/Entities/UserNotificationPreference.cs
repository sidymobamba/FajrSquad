using System.ComponentModel.DataAnnotations;

namespace FajrSquad.Core.Entities
{
    public class UserNotificationPreference : BaseEntity
    {
        [Key] 
        public Guid UserId { get; set; }
        
        public bool Morning { get; set; } = true;
        public bool Evening { get; set; } = true;
        public bool FajrMissed { get; set; } = true;
        public bool Escalation { get; set; } = true;
        public bool HadithDaily { get; set; } = true;
        public bool MotivationDaily { get; set; } = true;
        public bool EventsNew { get; set; } = true;
        public bool EventsReminder { get; set; } = true;
        
        public TimeSpan? QuietHoursStart { get; set; }
        public TimeSpan? QuietHoursEnd { get; set; }

        // Navigation property
        public User User { get; set; } = default!;
    }
}
