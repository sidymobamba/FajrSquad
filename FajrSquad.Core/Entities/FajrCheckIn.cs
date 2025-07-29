using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FajrSquad.Core.Enums;

namespace FajrSquad.Core.Entities
{
    public class FajrCheckIn
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = default!;

        public DateTime Date { get; set; } // solo data, no ora
        public CheckInStatus Status { get; set; }
    }

}
