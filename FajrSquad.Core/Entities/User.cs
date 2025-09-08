using System.ComponentModel.DataAnnotations;

namespace FajrSquad.Core.Entities
{
    public class User : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Phone]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(255)]
        public string? Email { get; set; }

        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string City { get; set; } = string.Empty;

        [StringLength(50)]
        public string Role { get; set; } = "User";

        [StringLength(100)]
        public string? MotivatingBrother { get; set; }

        public int FajrStreak { get; set; } = 0;

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        // Profile Picture
        [StringLength(500)]
        public string? ProfilePicture { get; set; }

        // Navigation Properties
        public ICollection<FajrCheckIn> CheckIns { get; set; } = new List<FajrCheckIn>();
        public UserSettings? Settings { get; set; }
    }
}