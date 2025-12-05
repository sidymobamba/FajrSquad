using System.ComponentModel.DataAnnotations;

namespace FajrSquad.Core.Entities.Adhkar
{
    public class UserAdhkarFavorite : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid AdhkarId { get; set; }

        // Navigation properties
        public virtual User? User { get; set; }
        public virtual Adhkar? Adhkar { get; set; }
    }
}



