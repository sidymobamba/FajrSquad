using System.Security.Claims;
using FajrSquad.Core.DTOs;
using FajrSquad.Core.Entities;
using FajrSquad.Core.Enums;
using FajrSquad.Infrastructure.Data;
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

        public FajrController(FajrDbContext db)
        {
            _db = db;
        }

        [Authorize]
        [HttpPost("checkin")]
        public async Task<IActionResult> CheckIn(CheckInRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var today = DateTime.UtcNow.Date;

            if (!Enum.IsDefined(typeof(CheckInStatus), request.Status) || request.Status == CheckInStatus.None)
                return BadRequest("Stato di check-in non valido.");

            var already = await _db.FajrCheckIns
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Date == today);

            if (already != null)
                return BadRequest("Hai già fatto check-in oggi.");

            var checkin = new FajrCheckIn
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Date = today,
                Status = request.Status
            };

            _db.FajrCheckIns.Add(checkin);
            await _db.SaveChangesAsync();

            var messages = new[]
            {
                "Allah ama chi si sveglia per Lui. 🌙",
                "Inizia la tua giornata con luce e benedizione.",
                "Ogni check-in è una vittoria sull'ego."
            };

            var randomMessage = messages[new Random().Next(messages.Length)];

            return Ok(new { message = "Check-in registrato.", inspiration = randomMessage });
        }



        [HttpGet("today-status")]
        public async Task<IActionResult> TodayStatus()
        {
            var today = DateTime.UtcNow.Date;

            var result = await _db.FajrCheckIns
                .Include(f => f.User)
                .Where(f => f.Date == today)
                .Select(f => new {
                    f.User.Name,
                    f.User.City,
                    f.Status
                })
                .ToListAsync();

            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("missed-checkin")]
        public async Task<IActionResult> MissedCheckIn()
        {
            var today = DateTime.UtcNow.Date;
            var allUsers = await _db.Users.ToListAsync();
            var checkedIn = await _db.FajrCheckIns
                .Where(f => f.Date == today)
                .Select(f => f.UserId)
                .ToListAsync();

            var missed = allUsers
                .Where(u => !checkedIn.Contains(u.Id))
                .Select(u => new { u.Name, u.City });

            return Ok(missed);
        }

        [Authorize]
        [HttpGet("my-history")]
        public async Task<IActionResult> GetMyHistory()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var data = await _db.FajrCheckIns
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.Date)
                .Select(c => new CheckInHistoryDto
                {
                    Date = c.Date,
                    Status = c.Status.ToString()
                })
                .ToListAsync();

            return Ok(data);
        }

        [Authorize]
        [HttpGet("fajr-streak")]
        public async Task<IActionResult> GetFajrStreak()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var checkIns = await _db.FajrCheckIns
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
                else
                {
                    break;
                }
            }

            return Ok(new { streak });
        }

        [HttpGet("leaderboard/daily")]
        public async Task<IActionResult> GetDailyLeaderboard()
        {
            var today = DateTime.UtcNow.Date;

            var data = await _db.FajrCheckIns
                .Where(f => f.Date == today)
                .Include(f => f.User)
                .OrderBy(f => f.Status) // opzionale: mettere prima "OnTime"
                .Select(f => new
                {
                    f.User.Name,
                    f.User.City,
                    f.Status,
                    f.Date
                })
                .ToListAsync();

            return Ok(data);
        }
        [HttpGet("leaderboard/weekly")]
        public async Task<IActionResult> GetWeeklyLeaderboard()
        {
            var startOfWeek = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);

            var data = await _db.FajrCheckIns
                .Where(f => f.Date >= startOfWeek)
                .Include(f => f.User)
                .GroupBy(f => f.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Name = g.First().User.Name,
                    City = g.First().User.City,
                    Total = g.Count()
                })
                .OrderByDescending(x => x.Total)
                .ToListAsync();

            return Ok(data);
        }
        [Authorize]
        [HttpGet("has-checked-in")]
        public async Task<IActionResult> HasCheckedInToday()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var today = DateTime.UtcNow.Date;

            var hasCheckedIn = await _db.FajrCheckIns
                .AnyAsync(c => c.UserId == userId && c.Date == today);

            return Ok(new { hasCheckedIn });
        }

    }
}
