using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FajrSquad.Core.Entities
{
    public class DeviceToken
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public string Token { get; set; } = string.Empty;

    }
}
