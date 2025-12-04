using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FajrSquad.Core.Entities
{
    public class User : BaseEntity
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, Phone, StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress, StringLength(255)]
        public string? Email { get; set; }

        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string City { get; set; } = string.Empty;

        // 👇 NUOVO: Country salvato a DB (nome Paese, es. "Senegal", "Italy")
        [Required, StringLength(56)]
        public string Country { get; set; } = "Italy";

        // 👇 Location coordinates for accurate prayer times calculation
        [Column(TypeName = "decimal(9,6)")]
        public decimal? Latitude { get; set; }

        [Column(TypeName = "decimal(9,6)")]
        public decimal? Longitude { get; set; }

        [StringLength(50)]
        public string? TimeZone { get; set; } // e.g., "Europe/Rome", "Africa/Dakar"

        [StringLength(50)]
        public string Role { get; set; } = "User";

        [StringLength(100)]
        public string? MotivatingBrother { get; set; }

        public int FajrStreak { get; set; } = 0;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public string? ProfilePicture { get; set; }

        public ICollection<FajrCheckIn> FajrCheckIns { get; set; } = new List<FajrCheckIn>();
        public UserSettings? Settings { get; set; }
        public ICollection<DeviceToken> DeviceTokens { get; set; } = new List<DeviceToken>();
        public ICollection<UserNotificationPreference> UserNotificationPreferences { get; set; } = new List<UserNotificationPreference>();

        [InverseProperty(nameof(RefreshToken.User))]
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
