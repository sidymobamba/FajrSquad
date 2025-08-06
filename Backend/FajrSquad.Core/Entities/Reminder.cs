using System.ComponentModel.DataAnnotations;

namespace FajrSquad.Core.Entities
{
    public class Reminder : BaseEntity
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
        // Types: "sleep", "fajr", "fasting", "islamic_holiday", "general"

        [StringLength(100)]
        public string? Category { get; set; } 
        // Categories: "pre_sleep", "fajr_reminder", "ramadan", "eid", "hajj", etc.

        public DateTime? ScheduledDate { get; set; } // Pour les fêtes/événements spécifiques

        public TimeSpan? ScheduledTime { get; set; } // Heure de rappel

        public bool IsRecurring { get; set; } = false; // Récurrent (ex: chaque jour)

        [StringLength(50)]
        public string? RecurrencePattern { get; set; } // "daily", "weekly", "monthly", "yearly"

        public int Priority { get; set; } = 1; // 1=high, 2=medium, 3=low

        public bool IsActive { get; set; } = true;

        [StringLength(50)]
        public string Language { get; set; } = "fr";

        // Champs spécifiques pour les fêtes islamiques
        [StringLength(100)]
        public string? HijriDate { get; set; } // Date hijri pour les fêtes

        public bool IsHijriCalendar { get; set; } = false; // Si basé sur calendrier hijri

        // Métadonnées additionnelles
        [StringLength(500)]
        public string? AdditionalInfo { get; set; } // Infos supplémentaires (horaires, durée, etc.)

        [StringLength(200)]
        public string? ActionUrl { get; set; } // URL vers plus d'infos ou action
    }
}