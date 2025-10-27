using System.ComponentModel.DataAnnotations;

namespace FajrSquad.Core.DTOs.Adhkar
{
    public class AdhkarDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string[] Categories { get; set; } = Array.Empty<string>();
        public int Priority { get; set; }
        public int Repetitions { get; set; }
        public string? SourceBook { get; set; }
        public string? SourceRef { get; set; }
        public string? License { get; set; }
        public bool Visible { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public List<AdhkarTextDto> Texts { get; set; } = new();
    }

    public class AdhkarTextDto
    {
        public Guid AdhkarId { get; set; }
        public string Lang { get; set; } = default!;
        public string? TextAr { get; set; }
        public string? Transliteration { get; set; }
        public string? Translation { get; set; }
    }

    public class AdhkarSetDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string TitleIt { get; set; } = default!;
        public string Type { get; set; } = default!;
        public int Ord { get; set; }
        public string? EveningStart { get; set; }
        public string? EveningEnd { get; set; }
        public List<AdhkarSetItemDto> Items { get; set; } = new();
    }

    public class AdhkarSetItemDto
    {
        public Guid Id { get; set; }
        public Guid SetId { get; set; }
        public Guid AdhkarId { get; set; }
        public int Ord { get; set; }
        public int Repetitions { get; set; }
        public AdhkarDto Adhkar { get; set; } = default!;
    }

    public class UserAdhkarProgressDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateOnly DateUtc { get; set; }
        public string TzId { get; set; } = default!;
        public bool MorningCompleted { get; set; }
        public DateTimeOffset? MorningCompletedAt { get; set; }
        public Guid? MorningSetId { get; set; }
        public bool EveningCompleted { get; set; }
        public DateTimeOffset? EveningCompletedAt { get; set; }
        public Guid? EveningSetId { get; set; }
        public Dictionary<string, int> Counts { get; set; } = new();
    }

    // Request DTOs
    public class UpdateAdhkarCountRequest
    {
        [Required]
        public DateOnly DateUtc { get; set; }

        [Required]
        public string AdhkarCode { get; set; } = default!;

        [Required]
        [Range(-100, 100)]
        public int Delta { get; set; }
    }

    public class CompleteAdhkarWindowRequest
    {
        [Required]
        public DateOnly DateUtc { get; set; }

        [Required]
        public string Window { get; set; } = default!; // "morning" | "evening"

        [Required]
        public string SetCode { get; set; } = default!;
    }

    public class CreateBookmarkRequest
    {
        [Required]
        public string AdhkarCode { get; set; } = default!;

        [StringLength(500)]
        public string? Note { get; set; }
    }
}
