using System.ComponentModel.DataAnnotations;

namespace FajrSquad.Core.Entities.Adhkar
{
    public class UserAdhkarBookmark : BaseEntity
    {
        public Guid UserId { get; set; }

        public Guid AdhkarId { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }
    }
}
