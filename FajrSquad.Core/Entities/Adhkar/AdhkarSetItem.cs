using System.ComponentModel.DataAnnotations;

namespace FajrSquad.Core.Entities.Adhkar
{
    public class AdhkarSetItem : BaseEntity
    {
        public Guid SetId { get; set; }
        public AdhkarSet Set { get; set; } = default!;

        public Guid AdhkarId { get; set; }
        public Adhkar Adhkar { get; set; } = default!;

        public int Ord { get; set; } = 1;

        public int Repetitions { get; set; } = 1; // override
    }
}
