using FajrSquad.Core.DTOs;
using FajrSquad.Core.Entities;

namespace FajrSquad.Infrastructure.Services
{
    public interface IFajrService
    {
        Task<ServiceResult<CheckInResponse>> CheckInAsync(Guid userId, CheckInRequest request);
        Task<ServiceResult<UserStatsResponse>> GetUserStatsAsync(Guid userId);
        Task<ServiceResult<int>> GetStreakAsync(Guid userId);
        Task<ServiceResult<bool>> HasCheckedInTodayAsync(Guid userId);
        Task<ServiceResult<List<CheckInHistoryDto>>> GetHistoryAsync(Guid userId);
    }

    public class ServiceResult<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> ValidationErrors { get; set; } = new();

        public static ServiceResult<T> SuccessResult(T data) => new() { Success = true, Data = data };
        public static ServiceResult<T> ErrorResult(string error) => new() { Success = false, ErrorMessage = error };
        public static ServiceResult<T> ValidationErrorResult(List<string> errors) => new() { Success = false, ValidationErrors = errors };
    }
}