using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FajrSquad.Core.DTOs
{
    public class UserProfileDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? City { get; set; }
        public string? Role { get; set; }
        public DateTime RegisteredAt { get; set; }
        public string? ProfilePicture { get; set; }
        public bool HasAvatar { get; set; }
        public object? Stats { get; set; }
        public UserSettingsDto? Settings { get; set; }
    }

}
