using System.Security.Claims;
using FajrSquad.Core.DTOs;
using FajrSquad.Core.Entities;
using FajrSquad.Core.Enums;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FajrSquad.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FajrController : ControllerBase
    {
        private readonly FajrDbContext _db;
        private readonly IFajrService _fajrService;
        private readonly ILogger<FajrController> _logger;

        public FajrController(FajrDbContext db, IFajrService fajrService, ILogger<FajrController> logger)
        {
            _db = db;
            _fajrService = fajrService;
            _logger = logger;
        }

        [Authorize]
        [HttpPost("checkin")]
        public async Task<IActionResult> CheckIn(CheckInRequest request)
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                var result = await _fajrService.CheckInAsync(userId, request);

                if (!result.Success)
                {
                    if (result.ValidationErrors.Any())
                        return BadRequest(ApiResponse<object>.ValidationErrorResponse(result.ValidationErrors));
                    
                    return BadRequest(ApiResponse<object>.ErrorResponse(result.ErrorMessage!));
                }

                return Ok(ApiResponse<CheckInResponse>.SuccessResponse(result.Data!, "Check-in completato con successo"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during check-in");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }





        [Authorize]
        [HttpGet("user-stats")]
        public async Task<IActionResult> GetUserStats()
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                var result = await _fajrService.GetUserStatsAsync(userId);

                if (!result.Success)
                    return BadRequest(ApiResponse<object>.ErrorResponse(result.ErrorMessage!));

                return Ok(ApiResponse<UserStatsResponse>.SuccessResponse(result.Data!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user stats");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [Authorize]
        [HttpGet("my-history")]
        public async Task<IActionResult> GetMyHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 30)
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                var result = await _fajrService.GetHistoryAsync(userId);

                if (!result.Success)
                    return BadRequest(ApiResponse<object>.ErrorResponse(result.ErrorMessage!));

                // Apply pagination
                var totalCount = result.Data!.Count;
                var paginatedItems = result.Data!
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var paginatedResponse = new PaginatedResponse<CheckInHistoryDto>
                {
                    Items = paginatedItems,
                    TotalCount = totalCount,
                    PageNumber = page,
                    PageSize = pageSize
                };

                return Ok(ApiResponse<PaginatedResponse<CheckInHistoryDto>>.SuccessResponse(paginatedResponse));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user history");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [Authorize]
        [HttpGet("fajr-streak")]
        public async Task<IActionResult> GetFajrStreak()
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                var result = await _fajrService.GetStreakAsync(userId);

                if (!result.Success)
                    return BadRequest(ApiResponse<object>.ErrorResponse(result.ErrorMessage!));

                return Ok(ApiResponse<int>.SuccessResponse(result.Data, "Streak calcolato con successo"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting streak");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [Authorize]
        [HttpGet("has-checked-in")]
        public async Task<IActionResult> HasCheckedInToday()
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                var result = await _fajrService.HasCheckedInTodayAsync(userId);

                if (!result.Success)
                    return BadRequest(ApiResponse<object>.ErrorResponse(result.ErrorMessage!));

                return Ok(ApiResponse<bool>.SuccessResponse(result.Data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking today's check-in");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [HttpGet("leaderboard/daily")]
        public async Task<IActionResult> GetDailyLeaderboard([FromQuery] int limit = 10)
        {
            try
            {
                var today = DateTime.UtcNow.Date;

                var leaderboard = await _db.FajrCheckIns
                    .Where(f => f.Date == today)
                    .Include(f => f.User)
                    .OrderBy(f => f.Status)
                    .ThenBy(f => f.CreatedAt)
                    .Take(limit)
                    .Select((f, index) => new LeaderboardEntry
                    {
                        UserId = f.UserId,
                        Name = f.User.Name,
                        City = f.User.City,
                        Status = f.Status.ToString(),
                        Rank = index + 1,
                        Score = f.Status == CheckInStatus.OnTime ? 10 : f.Status == CheckInStatus.Late ? 5 : 0
                    })
                    .ToListAsync();

                return Ok(ApiResponse<List<LeaderboardEntry>>.SuccessResponse(leaderboard));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily leaderboard");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [HttpGet("leaderboard/weekly")]
        public async Task<IActionResult> GetWeeklyLeaderboard([FromQuery] int limit = 10)
        {
            try
            {
                var startOfWeek = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);

                var leaderboard = await _db.FajrCheckIns
                    .Where(f => f.Date >= startOfWeek)
                    .Include(f => f.User)
                    .GroupBy(f => f.UserId)
                    .Select(g => new LeaderboardEntry
                    {
                        UserId = g.Key,
                        Name = g.First().User.Name,
                        City = g.First().User.City,
                        Score = g.Count(),
                        Status = "Active"
                    })
                    .OrderByDescending(x => x.Score)
                    .Take(limit)
                    .ToListAsync();

                // Add ranking
                for (int i = 0; i < leaderboard.Count; i++)
                {
                    leaderboard[i].Rank = i + 1;
                }

                return Ok(ApiResponse<List<LeaderboardEntry>>.SuccessResponse(leaderboard));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weekly leaderboard");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [HttpGet("today-status")]
        public async Task<IActionResult> TodayStatus()
        {
            try
            {
                var today = DateTime.UtcNow.Date;

                var todayStatus = await _db.FajrCheckIns
                    .Include(f => f.User)
                    .Where(f => f.Date == today)
                    .Select(f => new
                    {
                        f.User.Name,
                        f.User.City,
                        Status = f.Status.ToString(),
                        CheckInTime = f.CreatedAt
                    })
                    .OrderBy(f => f.CheckInTime)
                    .ToListAsync();

                return Ok(ApiResponse<object>.SuccessResponse(todayStatus));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today status");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("missed-checkin")]
        public async Task<IActionResult> MissedCheckIn()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                
                var missedUsers = await _db.Users
                    .Where(u => !_db.FajrCheckIns.Any(f => f.UserId == u.Id && f.Date == today))
                    .Select(u => new { u.Name, u.City, u.Phone })
                    .ToListAsync();

                return Ok(ApiResponse<object>.SuccessResponse(new { 
                    count = missedUsers.Count, 
                    users = missedUsers 
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting missed check-ins");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        private bool TryGetUserId(out Guid userId)
        {
            userId = Guid.Empty;
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            
            if (string.IsNullOrEmpty(userIdClaim))
                return false;

            return Guid.TryParse(userIdClaim, out userId);
        }
    }
}
