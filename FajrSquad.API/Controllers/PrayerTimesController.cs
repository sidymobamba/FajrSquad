using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace FajrSquad.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrayerTimesController : ControllerBase
    {
        [HttpGet("fajr-countdown/{city}")]
        public async Task<IActionResult> GetFajrCountdown(string city)
        {
            // Calcolo semplificato - in produzione usare API come Aladhan
            var now = DateTime.UtcNow;
            var tomorrow = now.Date.AddDays(1);
            
            // Orario Fajr approssimativo (da personalizzare per città)
            var fajrTime = tomorrow.AddHours(5).AddMinutes(30); // 5:30 AM
            
            var timeUntilFajr = fajrTime - now;
            
            if (timeUntilFajr.TotalSeconds < 0)
            {
                // Se è già passato, calcola per domani
                fajrTime = fajrTime.AddDays(1);
                timeUntilFajr = fajrTime - now;
            }
            
            return Ok(new 
            {
                fajrTime = fajrTime.ToString("HH:mm"),
                countdown = new 
                {
                    hours = (int)timeUntilFajr.TotalHours,
                    minutes = timeUntilFajr.Minutes,
                    seconds = timeUntilFajr.Seconds
                }
            });
        }
    }
}