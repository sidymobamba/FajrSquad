using System.ComponentModel.DataAnnotations;

namespace FajrSquad.Core.DTOs
{
    public class UpdateProfileRequest
    {
        [StringLength(100, MinimumLength = 2)]
        public string? Name { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(50, MinimumLength = 2)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        [StringLength(100)]
        public string? MotivatingBrother { get; set; }
    }

    public class UpdateUserSettingsRequest
    {
        // Notification Settings
        public bool? FajrReminder { get; set; }
        public bool? MorningHadith { get; set; }
        public bool? EveningMotivation { get; set; }
        public bool? IslamicHolidays { get; set; }
        public bool? FastingReminders { get; set; }
        public bool? SleepReminders { get; set; }

        // Timing Settings
        public TimeSpan? FajrReminderTime { get; set; }
        public TimeSpan? MorningHadithTime { get; set; }
        public TimeSpan? EveningMotivationTime { get; set; }
        public TimeSpan? SleepReminderTime { get; set; }

        // Language & Localization
        [StringLength(10)]
        public string? Language { get; set; }

        [StringLength(50)]
        public string? Timezone { get; set; }

        // Privacy Settings
        public bool? ShowInLeaderboard { get; set; }
        public bool? AllowMotivatingBrotherNotifications { get; set; }
        public bool? ShareStreakPublicly { get; set; }

        // App Preferences
        public bool? DarkMode { get; set; }
        public bool? SoundEnabled { get; set; }
        public bool? VibrationEnabled { get; set; }

        [StringLength(20)]
        public string? NotificationSound { get; set; }
    }

    public class DeleteAccountRequest
    {
        [Required(ErrorMessage = "La password è richiesta per eliminare l'account")]
        public string Password { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Reason { get; set; } // Optional reason for deletion
    }
}