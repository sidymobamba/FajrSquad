using System.ComponentModel.DataAnnotations;

namespace FajrSquad.Core.Entities.Adhkar
{
    public class Adhkar : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string Code { get; set; } = default!; // UNIQUE

        public string[] Categories { get; set; } = Array.Empty<string>(); // {"morning","evening"}

        public int Priority { get; set; } = 100;

        public int Repetitions { get; set; } = 1;

        [StringLength(200)]
        public string? SourceBook { get; set; }

        [StringLength(100)]
        public string? SourceRef { get; set; }

        [StringLength(100)]
        public string? License { get; set; }

        [Required]
        [StringLength(64)]
        public string ContentHash { get; set; } = default!;

        public bool Visible { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public ICollection<AdhkarText> Texts { get; set; } = new List<AdhkarText>();
    }
}
