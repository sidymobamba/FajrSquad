using System.ComponentModel.DataAnnotations;

namespace FajrSquad.Core.Entities
{
    public class DeviceToken : BaseEntity
    {
        [Key] 
        public int Id { get; set; }
        
        [Required] 
        public Guid UserId { get; set; }
        
        [Required, MaxLength(512)] 
        public string Token { get; set; } = string.Empty;
        
        [MaxLength(20)] 
        public string Platform { get; set; } = "Android";
        
        [MaxLength(10)] 
        public string Language { get; set; } = "it";
        
        [MaxLength(100)] 
        public string TimeZone { get; set; } = "Africa/Dakar";
        
        [MaxLength(40)] 
        public string? AppVersion { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation property
        public User User { get; set; } = default!;
    }
}
