using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FajrSquad.Core.DTOs
{
    public class RegisterRequest
    {
        public string Name { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public string City { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string? Role { get; set; }
        public string? Email { get; set; }
    }


}
