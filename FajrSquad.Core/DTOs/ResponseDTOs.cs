using System;
using System.Collections.Generic;

namespace FajrSquad.Core.DTOs
{
    public class CheckInResponse
    {
        public string Message { get; set; } = string.Empty;
        public string Inspiration { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class UserStatsResponse
    {
        public int Level { get; set; }
        public int TotalCheckIns { get; set; }
        public int OnTimeCount { get; set; }
        public int Streak { get; set; }
        public int NextLevelProgress { get; set; }
        public int NextLevelTarget { get; set; }
        public double SuccessRate => TotalCheckIns > 0 ? (double)OnTimeCount / TotalCheckIns * 100 : 0;
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static ApiResponse<T> SuccessResponse(T data, string? message = null) => new()
        {
            Success = true,
            Data = data,
            Message = message
        };

        public static ApiResponse<T> ErrorResponse(string error) => new()
        {
            Success = false,
            Errors = new List<string> { error }
        };

        public static ApiResponse<T> ValidationErrorResponse(List<string> errors) => new()
        {
            Success = false,
            Errors = errors
        };
    }

    public class LeaderboardEntry
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int Score { get; set; }
        public string Status { get; set; } = string.Empty;
        public int Rank { get; set; }
    }

    public class PaginatedResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasNextPage => PageNumber < TotalPages;
        public bool HasPreviousPage => PageNumber > 1;
    }

    public sealed class PrayerWeekResponse
    {
        public string City { get; set; } = "";
        public string Country { get; set; } = "";
        public int Method { get; set; }
        public int School { get; set; }
        public string RangeStart { get; set; } = ""; // yyyy-MM-dd
        public string RangeEnd { get; set; } = "";   // yyyy-MM-dd
        public List<PrayerDayDto> Days { get; set; } = new();
    }

    public sealed class PrayerTodayResponse
    {
        public string City { get; set; } = "";
        public string Country { get; set; } = "";
        public string Date { get; set; } = ""; // yyyy-MM-dd
        public string Timezone { get; set; } = "";
        public PrayerTimesDto Prayers { get; set; } = new();
        public string? NextPrayerName { get; set; }
        public string? NextPrayerTime { get; set; } // HH:mm locale
        public string? NextFajrTime { get; set; }   // HH:mm locale (domani)
    }
}
