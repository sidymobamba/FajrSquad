using System.ComponentModel.DataAnnotations;

namespace FajrSquad.Core.Entities.Adhkar
{
    public class AdhkarSet : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string Code { get; set; } = default!;  // morning_default, evening_default

        [Required]
        [StringLength(200)]
        public string TitleIt { get; set; } = default!;

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = "morning"; // morning|evening|after_prayer|sleep

        public int Ord { get; set; } = 1;

        [StringLength(50)]
        public string? EveningStart { get; set; } // Asr|Maghrib

        [StringLength(50)]
        public string? EveningEnd { get; set; }   // IshaEnd|Midnight|Fajr

        public ICollection<AdhkarSetItem> Items { get; set; } = new List<AdhkarSetItem>();
    }
}
