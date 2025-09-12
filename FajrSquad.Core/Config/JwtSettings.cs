using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FajrSquad.Core.Config
{
    public class JwtSettings
    {
        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = "FajrSquad";
        public string Audience { get; set; } = "FajrSquadUsers";
        public int ExpirationMinutes { get; set; } = 60;  // durata access token
        public int RefreshTokenDays { get; set; } = 60;   // durata refresh token
    }

}
