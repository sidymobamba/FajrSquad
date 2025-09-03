using System.ComponentModel.DataAnnotations;

namespace FajrSquad.Core.Entities
{
    public class UserSettings : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }
        public User User { get; set; } = default!;

        // Notifications Settings
        public bool FajrReminder { get; set; } = true;
        public bool MorningHadith { get; set; } = true;
        public bool EveningMotivation { get; set; } = true;
        public bool IslamicHolidays { get; set; } = true;
        public bool FastingReminders { get; set; } = true;
        public bool SleepReminders { get; set; } = true;

        // Timing Settings
        public TimeSpan FajrReminderTime { get; set; } = new TimeSpan(4, 30, 0); // 4:30 AM
        public TimeSpan MorningHadithTime { get; set; } = new TimeSpan(6, 0, 0); // 6:00 AM
        public TimeSpan EveningMotivationTime { get; set; } = new TimeSpan(21, 0, 0); // 9:00 PM
        public TimeSpan SleepReminderTime { get; set; } = new TimeSpan(22, 0, 0); // 10:00 PM

        // Language & Localization
        [StringLength(10)]
        public string Language { get; set; } = "it"; // fr, ar, en

        [StringLength(50)]
        public string Timezone { get; set; } = "Europe/Italy";

        // Privacy Settings
        public bool ShowInLeaderboard { get; set; } = true;
        public bool AllowMotivatingBrotherNotifications { get; set; } = true;
        public bool ShareStreakPublicly { get; set; } = true;

        // App Preferences
        public bool DarkMode { get; set; } = false;
        public bool SoundEnabled { get; set; } = true;
        public bool VibrationEnabled { get; set; } = true;

        [StringLength(20)]
        public string NotificationSound { get; set; } = "default";
    }
}