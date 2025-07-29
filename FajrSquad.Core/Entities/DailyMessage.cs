using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FajrSquad.Core.Entities
{
    public class DailyMessage
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public string Message { get; set; } = string.Empty;
    }

}
