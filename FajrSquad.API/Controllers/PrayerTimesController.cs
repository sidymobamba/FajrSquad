using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Security.Claims;

namespace FajrSquad.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrayerTimesController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public PrayerTimesController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        [Authorize]
        [HttpGet("today")]
        public async Task<IActionResult> GetPrayerTimesToday([FromQuery] string country = "Italy")
        {
            try
            {
                var city = User.FindFirstValue("city");
                if (string.IsNullOrWhiteSpace(city))
                    return BadRequest(new { error = "La città non è disponibile nel token dell'utente." });

                var todayUrl = $"https://api.aladhan.com/v1/timingsByCity?city={Uri.EscapeDataString(city)}&country={Uri.EscapeDataString(country)}&method=2";
                var response = await _httpClient.GetAsync(todayUrl);
                if (!response.IsSuccessStatusCode)
                    return StatusCode(500, new { error = "Errore durante la chiamata all'API Aladhan (oggi)." });

                var content = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(content);

                var timings = json["data"]?["timings"];
                var timezoneStr = json["data"]?["meta"]?["timezone"]?.ToString();
                if (timings == null || timezoneStr == null)
                    return BadRequest(new { error = "Dati non trovati nella risposta dell'API." });

                var today = DateTimeOffset.Now.DateTime.Date;
                var now = DateTimeOffset.UtcNow;
                var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(timezoneStr);

                // Preghiere di oggi
                var prayerNames = new[] { "Fajr", "Dhuhr", "Asr", "Maghrib", "Isha" };
                var prayerTimes = new Dictionary<string, string>();
                DateTimeOffset? nextPrayerTime = null;
                string? nextPrayerName = null;

                foreach (var name in prayerNames)
                {
                    var timeStr = timings[name]?.ToString();
                    if (timeStr == null) continue;

                    var dateTime = DateTimeOffset.Parse($"{today:yyyy-MM-dd}T{timeStr}:00")
                        .ToOffset(tzInfo.GetUtcOffset(today));

                    prayerTimes[name] = dateTime.ToLocalTime().ToString("HH:mm");

                    if (nextPrayerTime == null && dateTime > now)
                    {
                        nextPrayerTime = dateTime;
                        nextPrayerName = name;
                    }
                }

                var countdown = nextPrayerTime.HasValue ? (nextPrayerTime.Value - now) : TimeSpan.Zero;

                // Calcolo prossimo Fajr (domani)
                var tomorrow = today.AddDays(1).ToString("dd-MM-yyyy"); // formato richiesto da Aladhan
                var tomorrowUrl = $"https://api.aladhan.com/v1/timingsByCity?city={Uri.EscapeDataString(city)}&country={Uri.EscapeDataString(country)}&method=2&date={tomorrow}";
                var fajrResponse = await _httpClient.GetAsync(tomorrowUrl);
                DateTimeOffset? nextFajrTime = null;

                if (fajrResponse.IsSuccessStatusCode)
                {
                    var fajrContent = await fajrResponse.Content.ReadAsStringAsync();
                    var fajrJson = JObject.Parse(fajrContent);
                    var fajrStr = fajrJson["data"]?["timings"]?["Fajr"]?.ToString();
                    if (fajrStr != null)
                    {
                        nextFajrTime = DateTimeOffset.Parse($"{today.AddDays(1):yyyy-MM-dd}T{fajrStr}:00")
                            .ToOffset(tzInfo.GetUtcOffset(today.AddDays(1)));
                    }
                }

                var nextFajrCountdown = nextFajrTime.HasValue ? (nextFajrTime.Value - now) : TimeSpan.Zero;

                return Ok(new
                {
                    city,
                    date = today.ToString("yyyy-MM-dd"),
                    timezone = timezoneStr,
                    prayers = prayerTimes,
                    nextPrayer = nextPrayerTime == null ? null : new
                    {
                        name = nextPrayerName,
                        time = nextPrayerTime.Value.ToLocalTime().ToString("HH:mm"),
                        countdown = new
                        {
                            hours = (int)countdown.TotalHours,
                            minutes = countdown.Minutes,
                            seconds = countdown.Seconds
                        }
                    },
                    nextFajr = nextFajrTime == null ? null : new
                    {
                        time = nextFajrTime.Value.ToLocalTime().ToString("HH:mm"),
                        countdown = new
                        {
                            hours = (int)nextFajrCountdown.TotalHours,
                            minutes = nextFajrCountdown.Minutes,
                            seconds = nextFajrCountdown.Seconds
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Errore interno del server", details = ex.Message });
            }
        }

        [HttpGet("week")]
        [AllowAnonymous] // 👉 così puoi testare senza token. Quando hai auth pronta rimetti [Authorize].
        public async Task<IActionResult> GetPrayerTimesWeek([FromQuery] string country = "Italy", [FromQuery] string? cityOverride = null)
        {
            // Se il JWT ha la claim "city", usala
            var cityFromToken = User?.FindFirstValue("city");

            // Fallback: se non c’è nel token, prova querystring, se no default
            var city = !string.IsNullOrWhiteSpace(cityFromToken)
                ? cityFromToken
                : !string.IsNullOrWhiteSpace(cityOverride)
                    ? cityOverride
                    : "Brescia"; // fallback di default

            var today = DateTime.UtcNow.Date;
            var results = new List<object>();

            for (int i = 0; i < 7; i++)
            {
                var date = today.AddDays(i).ToString("dd-MM-yyyy");
                var url = $"https://api.aladhan.com/v1/timingsByCity?city={city}&country={country}&method=2&date={date}";

                try
                {
                    var response = await _httpClient.GetStringAsync(url);
                    var json = JObject.Parse(response);
                    var fajr = json["data"]?["timings"]?["Fajr"]?.ToString();
                    results.Add(new { date, fajr });
                }
                catch (Exception ex)
                {
                    // fallback: se l’API esterna rompe, metti null
                    Console.WriteLine($"Errore API Aladhan {date}: {ex.Message}");
                    results.Add(new { date, fajr = (string?)null });
                }
            }

            return Ok(new { city, week = results });
        }



    }
}
