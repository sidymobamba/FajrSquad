using System.ComponentModel.DataAnnotations;

namespace FajrSquad.Core.Entities
{
    public class Motivation : BaseEntity
    {
        [Required]
        [StringLength(1000)]
        public string Text { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Type { get; set; } = string.Empty; // night, morning, general, fajr

        [StringLength(100)]
        public string? Theme { get; set; } // motivation, encouragement, spiritual

        public int Priority { get; set; } = 1; // 1=high, 2=medium, 3=low

        public bool IsActive { get; set; } = true;

        [StringLength(50)]
        public string Language { get; set; } = "fr"; // fr, ar, en

        [StringLength(200)]
        public string? Author { get; set; } // Optional author/source
    }
}