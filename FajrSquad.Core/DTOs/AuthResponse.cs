using System;

namespace FajrSquad.Core.DTOs
{
    public class AuthResponse
    {
        // === Access token (JWT) ===
        public string AccessToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiresAt { get; set; }    // UTC
        public int ExpiresIn { get; set; }                    // secondi (es. 3600)

        // === Refresh token ===
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiresAt { get; set; }   // UTC

        // === Dati utente sintetici ===
        public UserSummaryDto User { get; set; } = new();
        public string TokenType { get; set; } = "Bearer";     // opzionale, default "Bearer"
    }

    public class UserSummaryDto
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Email { get; set; }
        public string Phone { get; set; } = default!;
        public string City { get; set; } = default!;
        public string Country { get; set; } = default!; 
        public string Role { get; set; } = default!;
        public string? Avatar { get; set; }
    }
}
