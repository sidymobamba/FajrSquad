using System.ComponentModel.DataAnnotations;

namespace FajrSquad.Core.DTOs.Adhkar
{
    public class CreateAdhkarRequest
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
        public string? Source { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty; // morning, evening, prayer, sleep

        [Range(1, 3)]
        public int Priority { get; set; } = 1;

        [StringLength(50)]
        public string Language { get; set; } = "it";
    }
}



