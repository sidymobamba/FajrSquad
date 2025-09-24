using System.ComponentModel.DataAnnotations;

namespace FajrSquad.Core.Entities
{
    public class NotificationLog : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        
        public Guid? UserId { get; set; }
        
        [MaxLength(100)] 
        public string Type { get; set; } = string.Empty;
        
        public string PayloadJson { get; set; } = "{}";
        
        [MaxLength(30)] 
        public string Result { get; set; } = "Sent";
        
        [MaxLength(200)] 
        public string? ProviderMessageId { get; set; }
        
        public string? Error { get; set; }
        
        public string? CollapsibleKey { get; set; }
        
        public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
        
        public int Retried { get; set; } = 0;

        // Navigation property
        public User? User { get; set; }
    }
}
