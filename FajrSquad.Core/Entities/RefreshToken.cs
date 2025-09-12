using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FajrSquad.Core.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; }

        [Required, StringLength(200)]
        public string Token { get; set; } = default!;

        [Required]
        public Guid UserId { get; set; } = default!;  // se User.Id è string
        // public Guid UserId { get; set; }            // usa Guid se User.Id è Guid

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = default!;

        public DateTime Expires { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public string? CreatedByIp { get; set; }

        public DateTime? Revoked { get; set; }
        public string? RevokedByIp { get; set; }
        public string? ReplacedByToken { get; set; }

        [NotMapped] public bool IsExpired => DateTime.UtcNow >= Expires;
        [NotMapped] public bool IsRevoked => Revoked != null;
        [NotMapped] public bool IsActive => !IsRevoked && !IsExpired;
    }
}
