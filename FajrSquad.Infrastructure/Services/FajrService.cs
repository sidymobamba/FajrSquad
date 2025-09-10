using FajrSquad.Core.DTOs;
using FajrSquad.Core.Entities;
using FajrSquad.Core.Enums;
using FajrSquad.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;

namespace FajrSquad.Infrastructure.Services
{
    public class FajrService : IFajrService
    {
        private readonly FajrDbContext _context;
        private readonly ILogger<FajrService> _logger;

        // Italia-first (come nel controller) + fallback Windows
        private const string DefaultTimeZone = "Europe/Rome";

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

                // Giorno locale (non UTC) per evitare mismatch
                var todayLocal = GetLocalToday();

                // Validation
                if (!Enum.IsDefined(typeof(CheckInStatus), request.Status) || request.Status == CheckInStatus.None)
                {
                    return ServiceResult<CheckInResponse>.ValidationErrorResult(
                        new List<string> { "Stato di check-in non valido" });
                }

                // Un solo check-in per giorno locale
                var existingCheckIn = await _context.FajrCheckIns
                    .FirstOrDefaultAsync(x => x.UserId == userId && x.Date == todayLocal);

                if (existingCheckIn != null)
                {
                    return ServiceResult<CheckInResponse>.ErrorResult("Check-in già effettuato oggi");
                }

                // Persisti anche l'istante preciso del check-in (UTC)
                var checkIn = new FajrCheckIn
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Date = todayLocal,                 // giorno locale
                    Status = request.Status,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow,
                    CheckInAtUtc = DateTime.UtcNow     // 👈 nuovo campo
                };

                _context.FajrCheckIns.Add(checkIn);
                await _context.SaveChangesAsync();

                var response = new CheckInResponse
                {
                    Message = "Check-in registrato con successo",
                    Inspiration = GetRandomInspiration(),
                    Date = todayLocal,
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

        public async Task<ServiceResult<bool>> HasCheckedInTodayAsync(Guid userId, DateTime today /* già locale, passato dal controller */)
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
                    .Take(30)
                    .Select(c => new CheckInHistoryDto
                    {
                        Date = c.Date,
                        Status = c.Status.ToString(),
                        Notes = c.Notes,
                        // 👇 opzionale ma utile per la UI (se hai aggiunto il campo nel DTO)
                        // CreatedAt = c.CheckInAtUtc ?? c.CreatedAt
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

        // ===== Helpers =====

        private async Task<int> CalculateStreakAsync(Guid userId)
        {
            // Ordina per data (giorno locale salvato in .Date)
            var days = await _context.FajrCheckIns
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.Date)
                .Select(c => c.Date.Date)
                .ToListAsync();

            var streak = 0;
            var currentLocal = GetLocalToday();

            foreach (var d in days)
            {
                if (d == currentLocal)
                {
                    streak++;
                    currentLocal = currentLocal.AddDays(-1);
                }
                else break;
            }

            return streak;
        }

        private static TimeZoneInfo ResolveTz(string? tz)
        {
            var candidate = string.IsNullOrWhiteSpace(tz) ? DefaultTimeZone : tz!;
            try { return TimeZoneInfo.FindSystemTimeZoneById(candidate); }
            catch (TimeZoneNotFoundException)
            {
                // Fallback Windows
                return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            }
        }

        private static DateTime GetLocalToday()
        {
            var tz = ResolveTz(null);
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
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
    }
}
