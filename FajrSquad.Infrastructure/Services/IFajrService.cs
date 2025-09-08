using FajrSquad.Core.DTOs;
using FajrSquad.Core.Entities;

namespace FajrSquad.Infrastructure.Services
{
    public interface IFajrService
    {
        Task<ServiceResult<CheckInResponse>> CheckInAsync(Guid userId, CheckInRequest request);
        Task<ServiceResult<UserStatsResponse>> GetUserStatsAsync(Guid userId);
        Task<ServiceResult<int>> GetStreakAsync(Guid userId);

        // 🔹 Aggiornato: aggiungo parametro today
        Task<ServiceResult<bool>> HasCheckedInTodayAsync(Guid userId, DateTime today);

        Task<ServiceResult<List<CheckInHistoryDto>>> GetHistoryAsync(Guid userId);
    }

    public class ServiceResult<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string Message { get; set; }
        public string? ErrorMessage { get; set; }
        public IEnumerable<string> Errors { get; set; }
        public List<string> ValidationErrors { get; set; } = new();

        public static ServiceResult<T> SuccessResult(T data)
        {
            return new ServiceResult<T> { Success = true, Data = data };
        }

        // 🔹 Nuovo overload con messaggio
        public static ServiceResult<T> SuccessResult(T data, string message)
        {
            return new ServiceResult<T> { Success = true, Data = data, Message = message };
        }

        public static ServiceResult<T> ErrorResult(string error)
        {
            return new ServiceResult<T>
            {
                Success = false,
                Errors = new[] { error }
            };
        }

        public static ServiceResult<T> ValidationErrorResult(List<string> errors)
            => new() { Success = false, ValidationErrors = errors };
    }
}
