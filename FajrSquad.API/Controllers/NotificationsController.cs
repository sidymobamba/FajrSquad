using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FajrSquad.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FirebaseAdmin.Messaging;
using FajrSquad.Infrastructure.Services;

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

        // ✅ INVIA NOTIFICA AL FRÈRE MOTIVATEUR
        [Authorize]
        [HttpPost("notify-motivating-brother")]
        public async Task<IActionResult> NotifyMotivatingBrother()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var today = DateTime.UtcNow.Date;

            // Verifica check-in
            var hasCheckedIn = await _db.FajrCheckIns
                .AnyAsync(c => c.UserId == userId && c.Date == today);

            if (hasCheckedIn)
                return BadRequest("Hai già fatto check-in oggi.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || string.IsNullOrEmpty(user.MotivatingBrother))
                return BadRequest("Frère motivateur non configurato.");

            // Trova il fratello motivatore
            var motivatingBrother = await _db.Users
                .FirstOrDefaultAsync(u =>
                    u.Phone == user.MotivatingBrother || u.Name == user.MotivatingBrother);

            if (motivatingBrother == null)
                return NotFound("Frère motivateur non trovato.");

            // Trova il suo token FCM
            var token = await _db.DeviceTokens
                .Where(t => t.UserId == motivatingBrother.Id)
                .Select(t => t.Token)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(token))
                return Ok(new { message = "Frère motivateur trovato, ma non ha token di notifica." });

            // Invia la notifica via Firebase
            var message = new Message
            {
                Token = token,
                Notification = new Notification
                {
                    Title = "Fratello da motivare 💪",
                    Body = $"{user.Name} ha bisogno del tuo supporto per il Fajr!"
                }
            };

            await FirebaseMessaging.DefaultInstance.SendAsync(message);

            return Ok(new { message = $"Notifica inviata a {motivatingBrother.Name}" });
        }

        // 📊 RESOCONTO SETTIMANALE DELL'UTENTE
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

        // 🧪 TEST: invia motivazione (per cron job test manuale)
        [Authorize(Roles = "Admin")]
        [HttpPost("test-motivation")]
        public async Task<IActionResult> TestMotivation(
            [FromServices] NotificationService notificationService)
        {
            await notificationService.SendMotivationNotification("fajr"); // oppure: "afternoon", "night"
            return Ok("Motivazione inviata con successo");
        }

        // 🧪 TEST: invia hadith (per cron job test manuale)
        [Authorize(Roles = "Admin")]
        [HttpPost("test-hadith")]
        public async Task<IActionResult> TestHadith(
            [FromServices] NotificationService notificationService)
        {
            await notificationService.SendHadithNotification();
            return Ok("Hadith inviato con successo");
        }
    }
}
