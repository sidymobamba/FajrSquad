using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FajrSquad.Core.DTOs
{
    public class UpdateUserRequest
    {
        public string? Name { get; set; }
        public string? City { get; set; }
        public string? Role { get; set; } // solo per admin
        public string? Email { get; set; }
    }
}
