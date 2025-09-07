using FajrSquad.Core.DTOs;
using FajrSquad.Core.Entities;
using FajrSquad.Core.Enums;
using FajrSquad.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FajrSquad.Infrastructure.Services
{
    public class FajrService : IFajrService
    {
        private readonly FajrDbContext _context;
        private readonly ILogger<FajrService> _logger;

        public FajrService(FajrDbContext context, ILogger<FajrService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ServiceResult<CheckInResponse>> CheckInAsync(Guid userId, CheckInRequest request)
        {
            try
            {
                _logger.LogInformation("Processing check-in for user {UserId}", userId);

                var today = DateTime.UtcNow.Date;

                // Validation
                if (!Enum.IsDefined(typeof(CheckInStatus), request.Status) || request.Status == CheckInStatus.None)
                {
                    return ServiceResult<CheckInResponse>.ValidationErrorResult(new List<string> { "Stato di check-in non valido" });
                }

                // Check if already checked in
                var existingCheckIn = await _context.FajrCheckIns
                    .FirstOrDefaultAsync(x => x.UserId == userId && x.Date == today);

                if (existingCheckIn != null)
                {
                    return ServiceResult<CheckInResponse>.ErrorResult("Check-in già effettuato oggi");
                }

                // Create check-in
                var checkIn = new FajrCheckIn
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Date = today,
                    Status = request.Status,
                    CreatedAt = DateTime.UtcNow
                };

                _context.FajrCheckIns.Add(checkIn);
                await _context.SaveChangesAsync();

                var response = new CheckInResponse
                {
                    Message = "Check-in registrato con successo",
                    Inspiration = GetRandomInspiration(),
                    Date = today,
                    Status = request.Status.ToString()
                };

                _logger.LogInformation("Check-in completed successfully for user {UserId}", userId);
                return ServiceResult<CheckInResponse>.SuccessResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during check-in for user {UserId}", userId);
                return ServiceResult<CheckInResponse>.ErrorResult("Errore interno durante il check-in");
            }
        }

        public async Task<ServiceResult<UserStatsResponse>> GetUserStatsAsync(Guid userId)
        {
            try
            {
                var totalCheckIns = await _context.FajrCheckIns.CountAsync(c => c.UserId == userId);
                var onTimeCount = await _context.FajrCheckIns.CountAsync(c => c.UserId == userId && c.Status == CheckInStatus.OnTime);
                var streak = await CalculateStreakAsync(userId);

                var level = (totalCheckIns / 10) + 1;
                var nextLevelProgress = totalCheckIns % 10;

                var response = new UserStatsResponse
                {
                    Level = level,
                    TotalCheckIns = totalCheckIns,
                    OnTimeCount = onTimeCount,
                    Streak = streak,
                    NextLevelProgress = nextLevelProgress,
                    NextLevelTarget = 10
                };

                return ServiceResult<UserStatsResponse>.SuccessResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user stats for {UserId}", userId);
                return ServiceResult<UserStatsResponse>.ErrorResult("Errore nel recupero delle statistiche");
            }
        }

        private async Task<int> CalculateStreakAsync(Guid userId)
        {
            var checkIns = await _context.FajrCheckIns
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.Date)
                .Select(c => c.Date.Date)
                .ToListAsync();

            var streak = 0;
            var current = DateTime.UtcNow.Date;

            foreach (var date in checkIns)
            {
                if (date == current)
                {
                    streak++;
                    current = current.AddDays(-1);
                }
                else break;
            }

            return streak;
        }

        private static string GetRandomInspiration()
        {
            var messages = new[]
            {
                "Allah ama chi si sveglia per Lui. 🌙",
                "Inizia la tua giornata con luce e benedizione.",
                "Ogni check-in è una vittoria sull'ego.",
                "La preghiera del Fajr illumina il cuore.",
                "Barakallahu feek, fratello!"
            };

            return messages[Random.Shared.Next(messages.Length)];
        }

        public async Task<ServiceResult<int>> GetStreakAsync(Guid userId)
        {
            try
            {
                var streak = await CalculateStreakAsync(userId);
                return ServiceResult<int>.SuccessResult(streak);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating streak for {UserId}", userId);
                return ServiceResult<int>.ErrorResult("Errore nel calcolo dello streak");
            }
        }

        public async Task<ServiceResult<bool>> HasCheckedInTodayAsync(Guid userId, DateTime today)
        {
            try
            {
                var hasCheckIn = await _context.FajrCheckIns
                    .AnyAsync(f => f.UserId == userId && f.Date == today);

                return ServiceResult<bool>.SuccessResult(hasCheckIn);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking today's check-in");
                return ServiceResult<bool>.ErrorResult("Errore durante il controllo del check-in odierno");
            }
        }


        public async Task<ServiceResult<List<CheckInHistoryDto>>> GetHistoryAsync(Guid userId)
        {
            try
            {
                var history = await _context.FajrCheckIns
                    .Where(c => c.UserId == userId)
                    .OrderByDescending(c => c.Date)
                    .Take(30) // Limit to last 30 days
                    .Select(c => new CheckInHistoryDto
                    {
                        Date = c.Date,
                        Status = c.Status.ToString()
                    })
                    .ToListAsync();

                return ServiceResult<List<CheckInHistoryDto>>.SuccessResult(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting history for {UserId}", userId);
                return ServiceResult<List<CheckInHistoryDto>>.ErrorResult("Errore nel recupero dello storico");
            }
        }
    }
}