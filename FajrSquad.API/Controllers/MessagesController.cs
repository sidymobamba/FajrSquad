using System.Security.Claims;
using FajrSquad.Core.Entities;
using FajrSquad.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FajrSquad.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly FajrDbContext _db;

        public MessagesController(FajrDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages()
        {

            var messages = await _db.DailyMessages.ToListAsync();

            if (messages == null || messages.Count == 0)
                return NotFound("Nessun messaggio disponibile.");

            return Ok(messages);

        }


        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> SetDailyMessage([FromBody] DailyMessage request)
        {
            var existing = await _db.DailyMessages.FirstOrDefaultAsync(m => m.Date == request.Date.Date);

            if (existing != null)
            {
                existing.Message = request.Message;
            }
            else
            {
                _db.DailyMessages.Add(new DailyMessage
                {
                    Id = Guid.NewGuid(),
                    Date = request.Date.Date,
                    Message = request.Message
                });
            }

            await _db.SaveChangesAsync();
            return Ok("Messaggio giornaliero salvato.");
        }

        [HttpGet("recap-weekly")]
        public async Task<IActionResult> GetWeeklyRecap()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var startOfWeek = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);

            var data = await _db.FajrCheckIns
                .Where(f => f.UserId == userId && f.Date >= startOfWeek)
                .GroupBy(f => f.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            return Ok(data);
        }
        [HttpGet("random")]
        public async Task<IActionResult> GetRandomMessage()
        {
            try
            {
                var count = await _db.DailyMessages.CountAsync();
                if (count == 0)
                    return NotFound("Nessun messaggio disponibile.");

                var skip = new Random().Next(0, count);
                var message = await _db.DailyMessages.Skip(skip).Take(1).FirstOrDefaultAsync();

                if (message == null)
                    return StatusCode(500, "Errore: messaggio non trovato dopo lo skip.");

                if (string.IsNullOrWhiteSpace(message.Message))
                    return StatusCode(500, "Errore: il messaggio recuperato è nullo o vuoto.");

                return Ok(new { message = message.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Errore interno: {ex.Message}");
            }
        }

    }
}
