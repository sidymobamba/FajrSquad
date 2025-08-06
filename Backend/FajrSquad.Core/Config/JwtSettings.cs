using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FajrSquad.Core.Config
{
    public class JwtSettings
    {
        public string Secret { get; set; } = default!;
        public int ExpirationMinutes { get; set; } = 60;
        public string Issuer { get; set; } = "FajrSquad";
        public string Audience { get; set; } = "FajrSquadUsers";
    }

}
