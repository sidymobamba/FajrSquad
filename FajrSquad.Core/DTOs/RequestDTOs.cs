using System.ComponentModel.DataAnnotations;
using FajrSquad.Core.Enums;

namespace FajrSquad.Core.DTOs
{

    public record WeeklyDayDto(string Date, string Status, string? CheckinTime, string FajrTime);
    public record TrendDeltaDto(string Metric, int Delta);
    public record LeaderboardPreviewEntryDto(Guid UserId, string Name, string? City, int Rank, int Score);

    public record WeeklyRecapResponseDto(
        object Period, int WeeklyScore, int Completed, int Total,
        string AverageCheckinTime, int CurrentStreak, int BestStreak,
        List<WeeklyDayDto> WeekDays, List<TrendDeltaDto> Trends,
        List<LeaderboardPreviewEntryDto> LeaderboardTop
    );

    public class CheckInRequest
    {
        [Required(ErrorMessage = "Lo stato del check-in è obbligatorio")]
        public CheckInStatus Status { get; set; }
        
        public string? Notes { get; set; }
    }

    public class RegisterRequest
    {
        [Required(ErrorMessage = "Il nome è obbligatorio")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Il nome deve essere tra 2 e 100 caratteri")]
        public string Name { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email non valida")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Il telefono è obbligatorio")]
        [Phone(ErrorMessage = "Numero di telefono non valido")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "La città è obbligatoria")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "La città deve essere tra 2 e 50 caratteri")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Il PIN è obbligatorio")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "Il PIN deve essere di 4 cifre")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "Il PIN deve contenere solo numeri")]
        public string Pin { get; set; } = string.Empty;
    }

    public class RegisterWithOtpRequest
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

        [Required(ErrorMessage = "L'OTP è obbligatorio")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "L'OTP deve essere di 6 cifre")]
        public string Otp { get; set; } = string.Empty;

        [StringLength(100)]
        public string? MotivatingBrother { get; set; }
    }

    public class LoginRequest
    {
        [Required(ErrorMessage = "Il telefono è obbligatorio")]
        [Phone]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Il PIN è obbligatorio")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "Il PIN deve essere di 4 cifre")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "Il PIN deve contenere solo numeri")]
        public string Pin { get; set; } = string.Empty;
    }

    public class LoginWithOtpRequest
    {
        [Required(ErrorMessage = "Il telefono è obbligatorio")]
        [Phone]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'OTP è obbligatorio")]
        [StringLength(6, MinimumLength = 6)]
        public string Otp { get; set; } = string.Empty;
    }

    public class SendOtpRequest
    {
        [Required(ErrorMessage = "Il telefono è obbligatorio")]
        [Phone(ErrorMessage = "Numero di telefono non valido")]
        public string Phone { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Il vecchio PIN è obbligatorio")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "Il PIN deve essere di 4 cifre")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "Il PIN deve contenere solo numeri")]
        public string OldPin { get; set; } = string.Empty;

        [Required(ErrorMessage = "Il nuovo PIN è obbligatorio")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "Il PIN deve essere di 4 cifre")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "Il PIN deve contenere solo numeri")]
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
    }

    public class DeviceTokenDto
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }

    public class ProblemReportDto
    {
        [Required(ErrorMessage = "Il messaggio è obbligatorio")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Il messaggio deve essere tra 10 e 1000 caratteri")]
        public string Message { get; set; } = string.Empty;
    }

    public record MonthlyLeaderboardEntryDto(
    Guid UserId, string Name, string? City,
    int Rank, int Points, int OnTime, int Late, int Missed,
    string AvgCheckinTime, int EffortScore
    );

    public record MonthlyLeaderboardResponseDto(
        object ScopeMeta, List<MonthlyLeaderboardEntryDto> Entries,
        object Me, object Paging
    );

}