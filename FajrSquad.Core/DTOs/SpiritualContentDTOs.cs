using System.ComponentModel.DataAnnotations;

namespace FajrSquad.Core.DTOs
{
    // Hadith DTOs
    public class CreateHadithRequest
    {
        [Required]
        [StringLength(2000)]
        public string Text { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string TextArabic { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Source { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Theme { get; set; }

        [Range(1, 3)]
        public int Priority { get; set; } = 1;

        [StringLength(50)]
        public string Language { get; set; } = "fr";
    }

    // Motivation DTOs
    public class CreateMotivationRequest
    {
        [Required]
        [StringLength(1000)]
        public string Text { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Type { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Theme { get; set; }

        [Range(1, 3)]
        public int Priority { get; set; } = 1;

        [StringLength(50)]
        public string Language { get; set; } = "fr";

        [StringLength(200)]
        public string? Author { get; set; }
    }

    // Reminder DTOs
    public class CreateReminderRequest
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Type { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Category { get; set; }

        public DateTime? ScheduledDate { get; set; }

        public TimeSpan? ScheduledTime { get; set; }

        public bool IsRecurring { get; set; } = false;

        [StringLength(50)]
        public string? RecurrencePattern { get; set; }

        [Range(1, 3)]
        public int Priority { get; set; } = 1;

        [StringLength(50)]
        public string Language { get; set; } = "fr";

        [StringLength(100)]
        public string? HijriDate { get; set; }

        public bool IsHijriCalendar { get; set; } = false;

        [StringLength(500)]
        public string? AdditionalInfo { get; set; }

        [StringLength(200)]
        public string? ActionUrl { get; set; }
    }
}