using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FajrSquad.Core.DTOs
{
    public class CheckInHistoryDto
    {
        public DateTime Date { get; set; }
        public string Status { get; set; } = default!;
    }
}
