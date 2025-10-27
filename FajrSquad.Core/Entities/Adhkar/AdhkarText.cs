using System.ComponentModel.DataAnnotations;

namespace FajrSquad.Core.Entities.Adhkar
{
    public class AdhkarText : BaseEntity
    {
        public Guid AdhkarId { get; set; }
        public Adhkar Adhkar { get; set; } = default!;

        [Required]
        [StringLength(10)]
        public string Lang { get; set; } = "ar"; // "ar","it","en","fr"

        [StringLength(2000)]
        public string? TextAr { get; set; }      // per ar

        [StringLength(2000)]
        public string? Transliteration { get; set; }

        [StringLength(2000)]
        public string? Translation { get; set; }
    }
}
