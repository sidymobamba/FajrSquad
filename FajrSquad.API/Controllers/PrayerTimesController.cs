using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using FajrSquad.Core.DTOs;

namespace FajrSquad.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrayerTimesController : ControllerBase
    {
        private readonly HttpClient _http;

        public PrayerTimesController(IHttpClientFactory httpClientFactory)
        {
            _http = httpClientFactory.CreateClient();
        }

        // ---------- Helpers ----------
        private static string? Claim(ClaimsPrincipal u, string type) => u.FindFirstValue(type);

        private static (string city, string country) ResolvePlace(ClaimsPrincipal user, string? cityOverride, string? countryOverride)
        {
            var city = !string.IsNullOrWhiteSpace(cityOverride) ? cityOverride : Claim(user, "city");
            var country = !string.IsNullOrWhiteSpace(countryOverride) ? countryOverride : (Claim(user, "country") ?? "Italy");

            if (string.IsNullOrWhiteSpace(city))
                throw new ArgumentException("City non disponibile (né nel token né in query).");

            return (city!, country!);
        }

        private static PrayerTimesDto MapTimings(JToken timings)
        {
            string Get(string key) => timings[key]?.ToString() ?? "";

            return new PrayerTimesDto
            {
                Fajr = Get("Fajr"),
                Sunrise = Get("Sunrise"),
                Dhuhr = Get("Dhuhr"),
                Asr = Get("Asr"),
                Maghrib = Get("Maghrib"),
                Isha = Get("Isha"),
                Imsak = timings["Imsak"]?.ToString(),
                Midnight = timings["Midnight"]?.ToString()
            };
        }

        // ---------- TODAY ----------
        [Authorize]
        [HttpGet("today")]
        public async Task<IActionResult> GetToday(
            [FromQuery] int method = 3,    // 3 = MWL (default)
            [FromQuery] int school = 0,    // 0 = Standard
            [FromQuery] string? cityOverride = null,
            [FromQuery] string? country = null)
        {
            try
            {
                var (city, countryFinal) = ResolvePlace(User, cityOverride, country);

                var url =
                    $"https://api.aladhan.com/v1/timingsByCity" +
                    $"?city={Uri.EscapeDataString(city)}" +
                    $"&country={Uri.EscapeDataString(countryFinal)}" +
                    $"&method={method}&school={school}";

                var res = await _http.GetAsync(url);
                if (!res.IsSuccessStatusCode)
                    return StatusCode(502, new { error = "Errore chiamando Aladhan (today)." });

                var json = JObject.Parse(await res.Content.ReadAsStringAsync());
                var data = json["data"]!;
                var tz = data["meta"]?["timezone"]?.ToString() ?? "UTC";
                var timings = MapTimings(data["timings"]!);

                // prossimo salah (best-effort su orari HH:mm)
                var names = new[] { "Fajr", "Dhuhr", "Asr", "Maghrib", "Isha" };
                string? nextName = null, nextTime = null;
                try
                {
                    var now = TimeZoneInfo.ConvertTime(DateTime.UtcNow,
                        TimeZoneInfo.FindSystemTimeZoneById(tz));

                    foreach (var n in names)
                    {
                        var t = data["timings"]?[n]?.ToString();
                        if (string.IsNullOrWhiteSpace(t)) continue;

                        var parts = t.Split(':');
                        if (parts.Length < 2) continue;

                        var local = new DateTime(now.Year, now.Month, now.Day,
                            int.Parse(parts[0]), int.Parse(parts[1]), 0, DateTimeKind.Unspecified);

                        if (local > now)
                        {
                            nextName = n;
                            nextTime = local.ToString("HH:mm");
                            break;
                        }
                    }
                }
                catch { /* safe fallback */ }

                // Fajr domani
                string? nextFajr = null;
                try
                {
                    var tomorrow = DateTime.UtcNow.Date.AddDays(1).ToString("dd-MM-yyyy");
                    var tUrl =
                        $"https://api.aladhan.com/v1/timingsByCity?date={tomorrow}" +
                        $"&city={Uri.EscapeDataString(city)}&country={Uri.EscapeDataString(countryFinal)}" +
                        $"&method={method}&school={school}";
                    var tRes = await _http.GetAsync(tUrl);
                    if (tRes.IsSuccessStatusCode)
                    {
                        var tJson = JObject.Parse(await tRes.Content.ReadAsStringAsync());
                        nextFajr = tJson["data"]?["timings"]?["Fajr"]?.ToString();
                    }
                }
                catch { }

                var payload = new PrayerTodayResponse
                {
                    City = city,
                    Country = countryFinal,
                    Date = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
                    Timezone = tz,
                    Prayers = timings,
                    NextPrayerName = nextName,
                    NextPrayerTime = nextTime,
                    NextFajrTime = nextFajr
                };

                return Ok(payload);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Errore interno server", details = ex.Message });
            }
        }

        // ---------- WEEK / INTERVAL ----------
        [Authorize]
        [HttpGet("week")]
        public async Task<IActionResult> GetWeek(
            [FromQuery] int method = 3,               // default MWL
            [FromQuery] int school = 0,               // default Standard
            [FromQuery] string? start = null,         // yyyy-MM-dd
            [FromQuery] int offset = 0,               // giorni da oggi
            [FromQuery] int days = 7,                 // quanti giorni (1..14)
            [FromQuery] string? cityOverride = null,
            [FromQuery] string? country = null)
        {
            try
            {
                var (city, countryFinal) = ResolvePlace(User, cityOverride, country);

                // calcolo start date
                DateTime startDateUtc;
                if (!string.IsNullOrWhiteSpace(start) && DateTime.TryParse(start, out var parsed))
                    startDateUtc = parsed.Date;
                else
                    startDateUtc = DateTime.UtcNow.Date.AddDays(offset);

                days = Math.Clamp(days, 1, 14);

                // Aladhan calendar è mensile: dobbiamo chiamare i mesi necessari
                var monthsToFetch = new HashSet<(int y, int m)>();
                var cursor = startDateUtc;
                for (int i = 0; i < days; i++)
                {
                    monthsToFetch.Add((cursor.Year, cursor.Month));
                    cursor = cursor.AddDays(1);
                }

                // Scarico e indicizzo per giorno
                var monthCache = new Dictionary<(int y, int m), JArray>();
                foreach (var (y, m) in monthsToFetch)
                {
                    var url =
                        $"https://api.aladhan.com/v1/calendarByCity/{y}/{m:00}" +
                        $"?city={Uri.EscapeDataString(city)}&country={Uri.EscapeDataString(countryFinal)}" +
                        $"&method={method}&school={school}";
                    var json = JObject.Parse(await _http.GetStringAsync(url));
                    monthCache[(y, m)] = (JArray?)json["data"] ?? new JArray();
                }

                // build risposta
                var daysOut = new List<PrayerDayDto>(days);
                var it = startDateUtc;
                for (int i = 0; i < days; i++, it = it.AddDays(1))
                {
                    var arr = monthCache[(it.Year, it.Month)];
                    var idx = it.Day - 1;
                    if (idx < 0 || idx >= arr.Count) continue;

                    var d = arr[idx];
                    var timings = d["timings"]!;
                    var hijri = d["date"]?["hijri"]?["date"]?.ToString() ?? "";
                    var greg = d["date"]?["gregorian"]?["date"]?.ToString() ?? "";
                    var tz = d["meta"]?["timezone"]?.ToString() ??
                             d["meta"]?["timezoneName"]?.ToString() ?? "UTC";

                    daysOut.Add(new PrayerDayDto
                    {
                        Gregorian = greg,
                        Hijri = hijri,
                        WeekdayEn = d["date"]?["gregorian"]?["weekday"]?["en"]?.ToString(),
                        WeekdayAr = d["date"]?["hijri"]?["weekday"]?["ar"]?.ToString(),
                        Prayers = MapTimings(timings),
                        Timezone = tz
                    });
                }

                var resp = new PrayerWeekResponse
                {
                    City = city,
                    Country = countryFinal,
                    Method = method,
                    School = school,
                    RangeStart = startDateUtc.ToString("yyyy-MM-dd"),
                    RangeEnd = startDateUtc.AddDays(days - 1).ToString("yyyy-MM-dd"),
                    Days = daysOut
                };

                return Ok(resp);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Errore interno server", details = ex.Message });
            }
        }
    }
}
