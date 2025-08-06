using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FajrSquad.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FajrSquad.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly FajrDbContext _db;

        public NotificationsController(FajrDbContext db)
        {
            _db = db;
        }

        [Authorize]
        [HttpPost("notify-motivating-brother")]
        public async Task<IActionResult> NotifyMotivatingBrother()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var today = DateTime.UtcNow.Date;

            // Verifica se l'utente ha già fatto check-in oggi
            var hasCheckedIn = await _db.FajrCheckIns
                .AnyAsync(c => c.UserId == userId && c.Date == today);

            if (hasCheckedIn)
                return BadRequest("Hai già fatto check-in oggi.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || string.IsNullOrEmpty(user.MotivatingBrother))
                return BadRequest("Frère motivateur non configurato.");

            // Trova il frère motivateur
            var motivatingBrother = await _db.Users
                .FirstOrDefaultAsync(u => u.Phone == user.MotivatingBrother || u.Name == user.MotivatingBrother);

            if (motivatingBrother == null)
                return NotFound("Frère motivateur non trovato.");

            // In produzione: inviare push notification
            Console.WriteLine($"Notifica inviata a {motivatingBrother.Name}: {user.Name} ha bisogno di motivazione per Fajr!");

            return Ok(new { message = "Notifica inviata al tuo frère motivateur" });
        }

        [Authorize]
        [HttpGet("weekly-recap")]
        public async Task<IActionResult> GetWeeklyRecap()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var startOfWeek = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);

            var weeklyStats = await _db.FajrCheckIns
                .Where(f => f.UserId == userId && f.Date >= startOfWeek)
                .GroupBy(f => f.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            var totalDays = (DateTime.UtcNow.Date - startOfWeek).Days + 1;
            var checkedInDays = weeklyStats.Sum(s => s.Count);
            var missedDays = totalDays - checkedInDays;

            return Ok(new 
            {
                weeklyStats,
                totalDays,
                checkedInDays,
                missedDays,
                successRate = totalDays > 0 ? (double)checkedInDays / totalDays * 100 : 0
            });
        }
    }
}