using System.ComponentModel.DataAnnotations;

namespace FajrSquad.Core.Entities.Adhkar
{
    public class UserAdhkarProgress : BaseEntity
    {
        public Guid UserId { get; set; }

        public DateOnly DateUtc { get; set; }

        [Required]
        [StringLength(100)]
        public string TzId { get; set; } = "Europe/Rome";

        public bool MorningCompleted { get; set; }

        public DateTimeOffset? MorningCompletedAt { get; set; }

        public Guid? MorningSetId { get; set; }

        public bool EveningCompleted { get; set; }

        public DateTimeOffset? EveningCompletedAt { get; set; }

        public Guid? EveningSetId { get; set; }

        public Dictionary<string, int> Counts { get; set; } = new(); // code -> count
    }
}
