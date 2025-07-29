using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FajrSquad.Core.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public string? Email { get; set; }

        public string PasswordHash { get; set; } = string.Empty;

        public string City { get; set; } = default!;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public int FajrStreak { get; set; } = 0;
        public string Role { get; set; }


        public ICollection<FajrCheckIn> CheckIns { get; set; } = new List<FajrCheckIn>();
    }

}
