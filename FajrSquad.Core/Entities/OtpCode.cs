using System.ComponentModel.DataAnnotations;

namespace FajrSquad.Core.Entities
{
    public class OtpCode : BaseEntity 
    {
        
        [Required]
        [Phone]
        public string Phone { get; set; } = string.Empty;
        
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Code { get; set; } = string.Empty;
        
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public User? User { get; set; }
    }
}