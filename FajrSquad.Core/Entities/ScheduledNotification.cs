using System.ComponentModel.DataAnnotations;

namespace FajrSquad.Core.Entities
{
    public class ScheduledNotification : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        
        public Guid? UserId { get; set; }
        
        [MaxLength(100)] 
        public string Type { get; set; } = string.Empty;
        
        public DateTimeOffset ExecuteAt { get; set; } // UTC
        
        public string DataJson { get; set; } = "{}";
        
        [MaxLength(20)] 
        public string Status { get; set; } = "Pending"; // Pending, Processing, Succeeded, Failed, SkippedNoActiveDevice, SkippedQuietHours, SkippedUserPreference
        
        [MaxLength(200)] 
        public string? UniqueKey { get; set; } // idempotenza
        
        public DateTimeOffset? ProcessedAt { get; set; }
        
        public string? ErrorMessage { get; set; }
        
        public int Retries { get; set; } = 0;
        
        public int MaxRetries { get; set; } = 3;
        
        public DateTimeOffset? NextRetryAt { get; set; }

        // Navigation property
        public User? User { get; set; }
    }
}
