using System.ComponentModel.DataAnnotations;

namespace FajrSquad.Core.Entities.Adhkar
{
    public class UserAdhkarProgress : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid AdhkarId { get; set; }

        [Required]
        public DateTime Date { get; set; } // Data del completamento

        [Range(0, 1000)]
        public int CurrentCount { get; set; } = 0; // Conteggio attuale (es. 2/3)

        public bool IsCompleted { get; set; } = false;

        public DateTime? CompletedAt { get; set; }

        // Navigation properties
        public virtual User? User { get; set; }
        public virtual Adhkar? Adhkar { get; set; }
    }
}

