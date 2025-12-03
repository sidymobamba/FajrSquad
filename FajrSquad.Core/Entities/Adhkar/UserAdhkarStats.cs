using System.ComponentModel.DataAnnotations;

namespace FajrSquad.Core.Entities.Adhkar
{
    public class UserAdhkarStats : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }

        public int TotalCompleted { get; set; } = 0;

        public int CurrentStreak { get; set; } = 0;

        public int LongestStreak { get; set; } = 0;

        public DateTime? LastCompletedDate { get; set; }

        // Navigation property
        public virtual User? User { get; set; }
    }
}

