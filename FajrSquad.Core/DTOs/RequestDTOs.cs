using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FajrSquad.Core.Enums;

namespace FajrSquad.Core.DTOs
{
    // ===== Weekly recap / leaderboard =====
    public record WeeklyDayDto(string Date, string Status, string? CheckinTime, string FajrTime);
    public record TrendDeltaDto(string Metric, int Delta);
    public record LeaderboardPreviewEntryDto(Guid UserId, string Name, string? City, int Rank, int Score);

    public record WeeklyRecapResponseDto(
        object Period, int WeeklyScore, int Completed, int Total,
        string AverageCheckinTime, int CurrentStreak, int BestStreak,
        List<WeeklyDayDto> WeekDays, List<TrendDeltaDto> Trends,
        List<LeaderboardPreviewEntryDto> LeaderboardTop
    );

    // ===== Check-in =====
    public class CheckInRequest
    {
        [Required(ErrorMessage = "Lo stato del check-in è obbligatorio")]
        public CheckInStatus Status { get; set; }

        public string? Notes { get; set; }
    }

    // ===== Auth: register / login =====
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Il nome è obbligatorio")]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Il telefono è obbligatorio")]
        [Phone]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "La città è obbligatoria")]
        [StringLength(50, MinimumLength = 2)]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Il PIN è obbligatorio")]
        [StringLength(4, MinimumLength = 4)]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "Il PIN deve contenere solo numeri")]
        public string Pin { get; set; } = string.Empty;
    }

    public class RegisterWithOtpRequest
    {
        [Required, StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        [Required, Phone]
        public string Phone { get; set; } = string.Empty;

        [Required, StringLength(50, MinimumLength = 2)]
        public string City { get; set; } = string.Empty;

        [Required, StringLength(6, MinimumLength = 6)]
        public string Otp { get; set; } = string.Empty;

        [StringLength(100)]
        public string? MotivatingBrother { get; set; }
    }

    public class LoginRequest
    {
        [Required, Phone]
        public string Phone { get; set; } = string.Empty;

        [Required, StringLength(4, MinimumLength = 4)]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "Il PIN deve contenere solo numeri")]
        public string Pin { get; set; } = string.Empty;
    }

    public class LoginWithOtpRequest
    {
        [Required, Phone]
        public string Phone { get; set; } = string.Empty;

        [Required, StringLength(6, MinimumLength = 6)]
        public string Otp { get; set; } = string.Empty;
    }

    public class SendOtpRequest
    {
        [Required, Phone]
        public string Phone { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        [Required, StringLength(4, MinimumLength = 4)]
        [RegularExpression(@"^\d{4}$")]
        public string OldPin { get; set; } = string.Empty;

        [Required, StringLength(4, MinimumLength = 4)]
        [RegularExpression(@"^\d{4}$")]
        public string NewPin { get; set; } = string.Empty;
    }

    public class UpdateUserRequest
    {
        [StringLength(100, MinimumLength = 2)]
        public string? Name { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(50, MinimumLength = 2)]
        public string? City { get; set; }

        public string? Role { get; set; }
    }

    public class CheckInHistoryDto
    {
        public DateTime Date { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class DeviceTokenDto
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }

    public class ProblemReportDto
    {
        [Required, StringLength(1000, MinimumLength = 10)]
        public string Message { get; set; } = string.Empty;
    }

    // ===== Mensile =====
    public record MonthlyLeaderboardEntryDto(
        Guid UserId, string Name, string? City,
        int Rank, int Points, int OnTime, int Late, int Missed,
        string AvgCheckinTime, int EffortScore
    );

    public record MonthlyLeaderboardResponseDto(
        object ScopeMeta, List<MonthlyLeaderboardEntryDto> Entries,
        object Me, object Paging
    );

    // ===== Refresh / Logout =====
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    // Puoi riusare AuthResponse come risposta del /auth/refresh
    public class RevokeRefreshTokenRequest
    {
        // opzionale se vuoi targettizzare un refresh specifico
        public string? RefreshToken { get; set; }
        // oppure logout globale: basta userId dal JWT
    }
}
