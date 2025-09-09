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

        // 🔹 per ora fisso, puoi sostituire con TimeZone preso da User
        private const string DefaultTimeZone = "Europe/Rome"; // oppure "Africa/Dakar"

        public FajrController(FajrDbContext db, IFajrService fajrService, ILogger<FajrController> logger)
        {
            _db = db;
            _fajrService = fajrService;
            _logger = logger;
        }

        // ----------------------------------------------------
        // 🔹 Check-In
        // ----------------------------------------------------
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

        // ----------------------------------------------------
        // 🔹 User Stats
        // ----------------------------------------------------
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

        // ----------------------------------------------------
        // 🔹 My History
        // ----------------------------------------------------
        [Authorize]
        [HttpGet("my-history")]
        public async Task<IActionResult> GetMyHistory(
     [FromQuery] int page = 1,
     [FromQuery] int pageSize = 30,
     [FromQuery] DateTime? start = null,
     [FromQuery] DateTime? end = null,
     [FromQuery] string? status = null)
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                var result = await _fajrService.GetHistoryAsync(userId);
                if (!result.Success)
                    return BadRequest(ApiResponse<object>.ErrorResponse(result.ErrorMessage!));

                var data = result.Data!.AsQueryable();

                if (start.HasValue) data = data.Where(x => x.Date >= start.Value.Date);
                if (end.HasValue) data = data.Where(x => x.Date <= end.Value.Date);

                if (!string.IsNullOrWhiteSpace(status))
                {
                    var allowed = status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    data = data.Where(x => allowed.Contains(x.Status));
                }

                var totalCount = data.Count();
                var items = data
                    .OrderByDescending(x => x.Date)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var paginatedResponse = new PaginatedResponse<CheckInHistoryDto>
                {
                    Items = items,
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

        // ----------------------------------------------------
        // 🔹 Streak
        // ----------------------------------------------------
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

        // ----------------------------------------------------
        // 🔹 Has Checked In Today
        // ----------------------------------------------------
        [Authorize]
        [HttpGet("has-checked-in")]
        public async Task<IActionResult> HasCheckedInToday()
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                var today = GetUserToday();

                var result = await _fajrService.HasCheckedInTodayAsync(userId, today);

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

        // ----------------------------------------------------
        // 🔹 Daily Leaderboard
        // ----------------------------------------------------
        [HttpGet("leaderboard/daily")]
        public async Task<IActionResult> GetDailyLeaderboard([FromQuery] int limit = 10)
        {
            try
            {
                var today = GetUserToday();

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


        [HttpGet("leaderboard/monthly")]
        public async Task<IActionResult> GetMonthlyLeaderboard(
    [FromQuery] int? year, [FromQuery] int? month,
    [FromQuery] int limit = 50, [FromQuery] int offset = 0,
    [FromQuery] string? tz = null)
        {
            try
            {
                // Fuso orario: default Europe/Rome + fallback Windows
                var tzId = string.IsNullOrWhiteSpace(tz) ? DefaultTimeZone : tz!;
                TimeZoneInfo tzInfo;
                try { tzInfo = TimeZoneInfo.FindSystemTimeZoneById(tzId); }
                catch (TimeZoneNotFoundException) { tzInfo = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time"); }
                catch (InvalidTimeZoneException) { tzInfo = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time"); }

                var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzInfo);

                var y = year ?? nowLocal.Year;
                var m = month ?? nowLocal.Month;
                var start = new DateTime(y, m, 1);
                var end = start.AddMonths(1).AddDays(-1);

                // Giorni trascorsi nel mese (per calcolare "missed")
                var lastLocalDay = nowLocal.Date < end.Date ? nowLocal.Day : end.Day;
                var daysElapsed = Math.Max(1, lastLocalDay); // evita /0

                // Query base
                var checkins = _db.FajrCheckIns
                    .Where(f => f.Date >= start && f.Date <= end)
                    .Include(f => f.User);

                var grouped = await checkins
                    .GroupBy(f => new { f.UserId, f.User.Name, f.User.City })
                    .Select(g => new {
                        g.Key.UserId,
                        g.Key.Name,
                        g.Key.City,
                        OnTime = g.Count(x => x.Status == CheckInStatus.OnTime),
                        Late = g.Count(x => x.Status == CheckInStatus.Late),
                        Times = g.Where(x => x.CreatedAt != null).Select(x => x.CreatedAt!)
                    })
                    .ToListAsync();

                // Media HH:mm nel fuso scelto
                string AvgTimeStr(IEnumerable<DateTime> utcTimes)
                {
                    var mins = utcTimes
                        .Select(t => TimeZoneInfo.ConvertTimeFromUtc(
                            DateTime.SpecifyKind(t, DateTimeKind.Utc), tzInfo))
                        .Select(dt => dt.Hour * 60 + dt.Minute)
                        .ToList();

                    if (mins.Count == 0) return "--:--";
                    var avgMin = (int)Math.Round(mins.Average());
                    return $"{avgMin / 60:00}:{avgMin % 60:00}";
                }

                // Best streak nel mese (OnTime/Late consecutivi)
                async Task<int> BestMonthlyStreakAsync(Guid userId)
                {
                    var days = await _db.FajrCheckIns
                        .Where(f => f.UserId == userId && f.Date >= start && f.Date <= end)
                        .Select(f => new { f.Date, f.Status })
                        .OrderBy(f => f.Date)
                        .ToListAsync();

                    int best = 0, cur = 0; DateTime? prev = null;
                    foreach (var d in days)
                    {
                        var ok = d.Status == CheckInStatus.OnTime || d.Status == CheckInStatus.Late;
                        if (ok)
                        {
                            if (prev.HasValue && (d.Date - prev.Value).TotalDays == 1) cur++; else cur = 1;
                            best = Math.Max(best, cur);
                        }
                        prev = d.Date;
                    }
                    return best;
                }

                // Materializza entries + effort
                var temp = new List<MonthlyLeaderboardEntryDto>();
                foreach (var g in grouped)
                {
                    var points = g.OnTime * 10 + g.Late * 5;
                    var withChk = g.OnTime + g.Late;
                    var missed = Math.Max(0, daysElapsed - withChk);
                    var avgStr = AvgTimeStr(g.Times);

                    var presenza = withChk / Math.Max(1.0, daysElapsed) * 100.0;
                    var ontime = g.OnTime / Math.Max(1.0, withChk) * 100.0;
                    var bestStreak = await BestMonthlyStreakAsync(g.UserId);
                    var consis = Math.Min(1.0, bestStreak / 7.0) * 100.0;

                    var effort = (0.60 * presenza) + (0.25 * ontime) + (0.15 * consis);
                    var effortScore = (int)Math.Round(Math.Clamp(effort, 0, 100));

                    temp.Add(new MonthlyLeaderboardEntryDto(
                        g.UserId, g.Name, g.City, 0, points, g.OnTime, g.Late, missed, avgStr, effortScore
                    ));
                }

                var ordered = temp
                    .OrderByDescending(e => e.Points)
                    .ThenByDescending(e => e.EffortScore)
                    .ThenBy(e => e.AvgCheckinTime == "--:--" ? "99:99" : e.AvgCheckinTime)
                    .ToList();

                var paged = ordered.Skip(offset).Take(limit).ToList();
                for (int i = 0; i < paged.Count; i++)
                    paged[i] = paged[i] with { Rank = offset + i + 1 };

                var meId = TryGetUserId(out var uid) ? uid : (Guid?)null;
                var me = meId.HasValue
                    ? ordered.Select((e, idx) => new { e, idx })
                             .FirstOrDefault(x => x.e.UserId == meId.Value)
                    : null;

                var response = new MonthlyLeaderboardResponseDto(
                    new { scope = "monthly", start = start.ToString("yyyy-MM-dd"), end = end.ToString("yyyy-MM-dd") },
                    paged,
                    me == null ? new { } : new { rank = me.idx + 1, points = me.e.Points, effortScore = me.e.EffortScore },
                    new { limit, offset, total = ordered.Count }
                );

                return Ok(ApiResponse<MonthlyLeaderboardResponseDto>.SuccessResponse(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monthly leaderboard");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        // ----------------------------------------------------
        // 🔹 Weekly Recap (Italia-first, orari localizzati, lun->dom)
        // ----------------------------------------------------
        [Authorize]
        [HttpGet("recap/weekly")]
        public async Task<IActionResult> GetWeeklyRecap([FromQuery] int offset = 0, [FromQuery] string? tz = null)
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                // Fuso orario con fallback Windows
                var tzInfo = ResolveTz(tz);
                DateTime ToLocal(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(utc, tzInfo);

                // finestra settimana [lun..dom]
                var todayLocal = ToLocal(DateTime.UtcNow).Date;
                var deltaToMonday = ((int)todayLocal.DayOfWeek + 6) % 7; // lun=0
                var monday = todayLocal.AddDays(-deltaToMonday + (offset * 7));
                var sunday = monday.AddDays(6);

                // storia personale (7gg) + settimana precedente (per trend)
                var myWeek = await _db.FajrCheckIns
                    .Where(f => f.UserId == userId && f.Date >= monday && f.Date <= sunday)
                    .OrderBy(f => f.Date)
                    .ToListAsync();

                var prevMon = monday.AddDays(-7);
                var prevSun = sunday.AddDays(-7);
                var myPrev = await _db.FajrCheckIns
                    .Where(f => f.UserId == userId && f.Date >= prevMon && f.Date <= prevSun)
                    .ToListAsync();

                // helper (solo in memoria, NON in query EF)
                bool IsDone(CheckInStatus s) => s == CheckInStatus.OnTime || s == CheckInStatus.Late;

                // giorni settimana per UI (orari convertiti nel fuso)
                var days = new List<WeeklyDayDto>();
                int completed = 0;
                var minutes = new List<int>();

                for (int i = 0; i < 7; i++)
                {
                    var d = monday.AddDays(i);
                    var entry = myWeek.FirstOrDefault(x => x.Date == d);

                    var status = entry == null
                        ? (d < todayLocal ? "Missed" : (d == todayLocal ? "Today" : "Future"))
                        : "Completed";

                    string? checkin = null;
                    if (entry?.CreatedAt != null)
                    {
                        var createdLocal = TimeZoneInfo.ConvertTimeFromUtc(
                            DateTime.SpecifyKind(entry.CreatedAt, DateTimeKind.Utc), tzInfo);
                        checkin = createdLocal.ToString("HH:mm");
                        minutes.Add(createdLocal.Hour * 60 + createdLocal.Minute);
                    }

                    if (entry != null && IsDone(entry.Status)) completed++;

                    days.Add(new WeeklyDayDto(d.ToString("yyyy-MM-dd"), status, checkin, "—"));
                }

                var weeklyScore = (int)Math.Round(completed / 7.0 * 100);
                var avg = minutes.Count == 0
                    ? "--:--"
                    : $"{(int)Math.Round(minutes.Average()) / 60:00}:{(int)Math.Round(minutes.Average()) % 60:00}";

                // streak dall’IFajrService
                var streakRes = await _fajrService.GetStreakAsync(userId);
                var currentStreak = streakRes.Success ? streakRes.Data : 0;

                // semplice best streak (su storico settimana attuale)
                int BestStreak(List<Core.Entities.FajrCheckIn> list)
                {
                    int best = 0, cur = 0; DateTime? prev = null;
                    foreach (var x in list.OrderBy(x => x.Date))
                    {
                        if (IsDone(x.Status))
                        {
                            if (prev.HasValue && (x.Date - prev.Value).TotalDays == 1) cur++; else cur = 1;
                            best = Math.Max(best, cur);
                        }
                        prev = x.Date;
                    }
                    return best;
                }

                var bestStreak = BestStreak(myWeek);
                var prevCompleted = myPrev.Count(x => IsDone(x.Status));
                var prevScore = (int)Math.Round(prevCompleted / 7.0 * 100);

                var trends = new List<TrendDeltaDto> {
            new("Completed", completed - prevCompleted),
            new("Score", weeklyScore - prevScore)
        };

                // mini leaderboard settimanale (top 5) — no funzioni locali in query EF
                var weeklyLb = await _db.FajrCheckIns
                    .Where(f => f.Date >= monday && f.Date <= sunday)
                    .GroupBy(f => new { f.UserId, f.User.Name, f.User.City })
                    .Select(g => new
                    {
                        g.Key.UserId,
                        Name = g.Key.Name,
                        City = g.Key.City,
                        Score = g.Count(x => x.Status == CheckInStatus.OnTime || x.Status == CheckInStatus.Late)
                    })
                    .OrderByDescending(x => x.Score)
                    .Take(5)
                    .ToListAsync();

                var preview = weeklyLb.Select((x, i) =>
                    new LeaderboardPreviewEntryDto(x.UserId, x.Name, x.City, i + 1, x.Score))
                    .ToList();

                var resp = new WeeklyRecapResponseDto(
                    new
                    {
                        label = offset == 0 ? "Settimana corrente" :
                                (offset == -1 ? "Settimana precedente" : $"Settimana {offset:+#;-#;0}"),
                        start = monday.ToString("yyyy-MM-dd"),
                        end = sunday.ToString("yyyy-MM-dd")
                    },
                    weeklyScore, completed, 7, avg, currentStreak, bestStreak, days, trends, preview
                );

                return Ok(ApiResponse<WeeklyRecapResponseDto>.SuccessResponse(resp));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weekly recap");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }


        // ----------------------------------------------------
        // 🔹 Weekly Leaderboard
        // ----------------------------------------------------
        // ----------------------------------------------------
        // 🔹 Weekly Leaderboard (lun->dom, no First() in Select)
        // ----------------------------------------------------
        [HttpGet("leaderboard/weekly")]
        public async Task<IActionResult> GetWeeklyLeaderboard([FromQuery] int limit = 10)
        {
            try
            {
                // oggi nel fuso di default (Italia) con fallback
                var tzInfo = ResolveTz(null);
                var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzInfo).Date;

                // lunedì della settimana
                var deltaToMonday = ((int)today.DayOfWeek + 6) % 7;
                var startOfWeek = today.AddDays(-deltaToMonday);

                var leaderboard = await _db.FajrCheckIns
                    .Where(f => f.Date >= startOfWeek && f.Date <= today)
                    .GroupBy(f => new { f.UserId, f.User.Name, f.User.City })
                    .Select(g => new LeaderboardEntry
                    {
                        UserId = g.Key.UserId,
                        Name = g.Key.Name,
                        City = g.Key.City,
                        // Conta solo OnTime/Late
                        Score = g.Count(x => x.Status == CheckInStatus.OnTime || x.Status == CheckInStatus.Late),
                        Status = "Active"
                    })
                    .OrderByDescending(x => x.Score)
                    .Take(limit)
                    .ToListAsync();

                for (int i = 0; i < leaderboard.Count; i++)
                    leaderboard[i].Rank = i + 1;

                return Ok(ApiResponse<List<LeaderboardEntry>>.SuccessResponse(leaderboard));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weekly leaderboard");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        // ----------------------------------------------------
        // 🔹 Today Status
        // ----------------------------------------------------
        // ----------------------------------------------------
        // 🔹 Today (Italia-first, orario localizzato)
        // ----------------------------------------------------
        [Authorize]
        [HttpGet("today")]
        public async Task<IActionResult> GetToday([FromQuery] string? tz = null)
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                var tzInfo = ResolveTz(tz);
                var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzInfo).Date;

                var entry = await _db.FajrCheckIns
                    .Where(f => f.UserId == userId && f.Date == today)
                    .FirstOrDefaultAsync();

                string? checkinTime = null;
                if (entry?.CreatedAt != null)
                {
                    var createdLocal = TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.SpecifyKind(entry.CreatedAt, DateTimeKind.Utc), tzInfo);
                    checkinTime = createdLocal.ToString("HH:mm");
                }

                return Ok(ApiResponse<object>.SuccessResponse(new
                {
                    date = today.ToString("yyyy-MM-dd"),
                    hasCheckedIn = entry != null,
                    status = entry?.Status.ToString(),
                    checkinTime
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        // ----------------------------------------------------
        // 🔹 Missed Check-In
        // ----------------------------------------------------
        // ----------------------------------------------------
        // 🔹 Missed Check-In (usa oggi nel fuso Italia di default)
        // ----------------------------------------------------
        [Authorize(Roles = "Admin")]
        [HttpGet("missed-checkin")]
        public async Task<IActionResult> MissedCheckIn()
        {
            try
            {
                var tzInfo = ResolveTz(null);
                var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzInfo).Date;

                var missedUsers = await _db.Users
                    .Where(u => !_db.FajrCheckIns.Any(f => f.UserId == u.Id && f.Date == today))
                    .Select(u => new { u.Name, u.City, u.Phone })
                    .ToListAsync();

                return Ok(ApiResponse<object>.SuccessResponse(new
                {
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

        // ----------------------------------------------------
        // 🔹 Helpers
        // ----------------------------------------------------
        private bool TryGetUserId(out Guid userId)
        {
            userId = Guid.Empty;
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

            if (string.IsNullOrEmpty(userIdClaim))
                return false;

            return Guid.TryParse(userIdClaim, out userId);
        }

        private DateTime GetUserToday()
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(DefaultTimeZone);
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
        }

        // ✅ Italia-first + fallback Windows
        private static TimeZoneInfo ResolveTz(string? tz)
        {
            var candidate = string.IsNullOrWhiteSpace(tz) ? "Europe/Rome" : tz!;
            try { return TimeZoneInfo.FindSystemTimeZoneById(candidate); }
            catch (TimeZoneNotFoundException)
            {
                // Windows fallback
                return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            }
        }

    }
}
