using System.ComponentModel.DataAnnotations;

namespace FajrSquad.Core.Entities.Adhkar
{
    public class Adhkar : BaseEntity
    {
        [Required]
        [StringLength(2000)]
        public string Arabic { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Transliteration { get; set; }

        [Required]
        [StringLength(2000)]
        public string Translation { get; set; } = string.Empty;

        [Required]
        [Range(1, 1000)]
        public int Repetitions { get; set; } = 1;

        [StringLength(200)]
        public string? Source { get; set; } // es. "Sahih Muslim 2692"

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty; // morning, evening, prayer, sleep

        [Range(1, 3)]
        public int Priority { get; set; } = 1; // 1=high, 2=medium, 3=low

        public bool IsActive { get; set; } = true;

        [StringLength(50)]
        public string Language { get; set; } = "it"; // it, fr, en
    }
}


