using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FajrSquad.Core.Entities
{
    public class User : BaseEntity  
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public string? MotivatingBrother { get; set; } 
        public int FajrStreak { get; set; } = 0;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        public ICollection<FajrCheckIn> CheckIns { get; set; } = new List<FajrCheckIn>();
    }


}
