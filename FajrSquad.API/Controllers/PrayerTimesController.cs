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
        public async Task<IActionResult> GetPrayerTimesToday([FromQuery] string? country = null, [FromQuery] string? cityOverride = null)
        {
            try
            {
                var city = !string.IsNullOrWhiteSpace(cityOverride)
                    ? cityOverride
                    : User.FindFirstValue("city");

                if (string.IsNullOrWhiteSpace(city))
                    return BadRequest(new { error = "La città non è disponibile (manca nel token e non è stata passata in query)." });

                var countryFinal = !string.IsNullOrWhiteSpace(country)
                    ? country
                    : (User.FindFirstValue("country") ?? "Italy");

                var today = DateTime.UtcNow.Date;
                var todayUrl =
                    $"https://api.aladhan.com/v1/timingsByCity?city={Uri.EscapeDataString(city)}&country={Uri.EscapeDataString(countryFinal)}&method=2";
                var response = await _httpClient.GetAsync(todayUrl);
                if (!response.IsSuccessStatusCode)
                    return StatusCode(500, new { error = "Errore durante la chiamata all'API Aladhan (oggi)." });

                var content = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(content);

                var timings = json["data"]?["timings"];
                var timezoneStr = json["data"]?["meta"]?["timezone"]?.ToString();
                if (timings == null || timezoneStr == null)
                    return BadRequest(new { error = "Dati non trovati nella risposta dell'API." });

                var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(timezoneStr);
                var nowUtc = DateTimeOffset.UtcNow;

                var prayerNames = new[] { "Fajr", "Dhuhr", "Asr", "Maghrib", "Isha" };
                var prayerTimes = new Dictionary<string, string>();
                DateTimeOffset? nextPrayerTime = null;
                string? nextPrayerName = null;

                foreach (var name in prayerNames)
                {
                    var timeStr = timings[name]?.ToString();
                    if (timeStr == null) continue;

                    var localDateTime = DateTime.SpecifyKind(today, DateTimeKind.Unspecified);
                    var dt = DateTimeOffset.Parse($"{localDateTime:yyyy-MM-dd}T{timeStr}:00");
                    var zoned = TimeZoneInfo.ConvertTime(dt, tzInfo);

                    prayerTimes[name] = zoned.ToLocalTime().ToString("HH:mm");

                    if (nextPrayerTime == null && zoned > nowUtc)
                    {
                        nextPrayerTime = zoned;
                        nextPrayerName = name;
                    }
                }

                var countdown = nextPrayerTime.HasValue ? (nextPrayerTime.Value - nowUtc) : TimeSpan.Zero;

                var tomorrow = today.AddDays(1).ToString("dd-MM-yyyy");
                var tomorrowUrl =
                    $"https://api.aladhan.com/v1/timingsByCity?city={Uri.EscapeDataString(city)}&country={Uri.EscapeDataString(countryFinal)}&method=2&date={tomorrow}";

                DateTimeOffset? nextFajrTime = null;
                var fajrResponse = await _httpClient.GetAsync(tomorrowUrl);
                if (fajrResponse.IsSuccessStatusCode)
                {
                    var fajrContent = await fajrResponse.Content.ReadAsStringAsync();
                    var fajrJson = JObject.Parse(fajrContent);
                    var fajrStr = fajrJson["data"]?["timings"]?["Fajr"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(fajrStr))
                    {
                        var localDateTime = DateTime.SpecifyKind(today.AddDays(1), DateTimeKind.Unspecified);
                        var dt = DateTimeOffset.Parse($"{localDateTime:yyyy-MM-dd}T{fajrStr}:00");
                        nextFajrTime = TimeZoneInfo.ConvertTime(dt, tzInfo);
                    }
                }

                var nextFajrCountdown = nextFajrTime.HasValue ? (nextFajrTime.Value - nowUtc) : TimeSpan.Zero;

                return Ok(new
                {
                    city,
                    country = countryFinal, 
                    date = today.ToString("yyyy-MM-dd"),
                    timezone = timezoneStr,
                    prayers = prayerTimes,
                    nextPrayer = nextPrayerTime == null ? null : new
                    {
                        name = nextPrayerName,
                        time = nextPrayerTime.Value.ToLocalTime().ToString("HH:mm"),
                        countdown = new { hours = (int)countdown.TotalHours, minutes = countdown.Minutes, seconds = countdown.Seconds }
                    },
                    nextFajr = nextFajrTime == null ? null : new
                    {
                        time = nextFajrTime.Value.ToLocalTime().ToString("HH:mm"),
                        countdown = new { hours = (int)nextFajrCountdown.TotalHours, minutes = nextFajrCountdown.Minutes, seconds = nextFajrCountdown.Seconds }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Errore interno del server", details = ex.Message });
            }
        }

        [HttpGet("week")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPrayerTimesWeek([FromQuery] string? country = null, [FromQuery] string? cityOverride = null)
        {
            var cityFromToken = User?.FindFirstValue("city");
            var city = !string.IsNullOrWhiteSpace(cityOverride) ? cityOverride
                     : !string.IsNullOrWhiteSpace(cityFromToken) ? cityFromToken
                     : "Brescia";

            var countryFinal = !string.IsNullOrWhiteSpace(country)
                ? country
                : (User?.FindFirstValue("country") ?? "Italy");

            var today = DateTime.UtcNow.Date;
            var results = new List<object>();

            for (int i = 0; i < 7; i++)
            {
                var date = today.AddDays(i).ToString("dd-MM-yyyy");
                var url = $"https://api.aladhan.com/v1/timingsByCity?city={Uri.EscapeDataString(city)}&country={Uri.EscapeDataString(countryFinal)}&method=2&date={date}";
                try
                {
                    var response = await _httpClient.GetStringAsync(url);
                    var json = JObject.Parse(response);
                    var fajr = json["data"]?["timings"]?["Fajr"]?.ToString();
                    results.Add(new { date, fajr });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Errore API Aladhan {date}: {ex.Message}");
                    results.Add(new { date, fajr = (string?)null });
                }
            }

            return Ok(new { city, country = countryFinal, week = results });
        }



    }
}
