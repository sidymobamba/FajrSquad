using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FajrSquad.Core.DTOs
{
    public class UserSettingsDto
    {
        public bool DarkMode { get; set; }
        public bool FajrReminder { get; set; }
        public TimeSpan? FajrReminderTime { get; set; }
        public bool SleepReminders { get; set; }
        public TimeSpan? SleepReminderTime { get; set; }
        public bool MorningHadith { get; set; }
        public TimeSpan? MorningHadithTime { get; set; }
        public bool EveningMotivation { get; set; }
        public TimeSpan? EveningMotivationTime { get; set; }
        public bool IslamicHolidays { get; set; }
        public string? Language { get; set; }
        public string? Timezone { get; set; }
        public bool AllowMotivatingBrotherNotifications { get; set; }
        public bool ShowInLeaderboard { get; set; }
        public bool ShareStreakPublicly { get; set; }
        public bool SoundEnabled { get; set; }
        public bool VibrationEnabled { get; set; }
        public string? NotificationSound { get; set; }
    }

}
