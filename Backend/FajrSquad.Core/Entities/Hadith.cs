using System.ComponentModel.DataAnnotations;

namespace FajrSquad.Core.Entities
{
    public class Hadith : BaseEntity
    {
        [Required]
        [StringLength(2000)]
        public string Text { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string TextArabic { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Source { get; set; } = string.Empty; // Bukhari, Muslim, etc.

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty; // morning, evening, general

        [StringLength(100)]
        public string? Theme { get; set; } // prayer, patience, charity, etc.

        public int Priority { get; set; } = 1; // 1=high, 2=medium, 3=low

        public bool IsActive { get; set; } = true;

        [StringLength(50)]
        public string Language { get; set; } = "it"; // fr, ar, en
    }
}