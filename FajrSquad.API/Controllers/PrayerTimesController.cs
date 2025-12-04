using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FajrSquad.Core.DTOs;
using FajrSquad.Infrastructure.Services.PrayerTimes;

namespace FajrSquad.API.Controllers
{
    /// <summary>
    /// Controller for prayer times with GPS-first location support
    /// Priority: GPS coordinates → city/country query → profile claims
    /// Isolated feature - no impact on login/register/auth endpoints
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PrayerTimesController : ControllerBase
    {
        private readonly IPrayerTimesService _prayerTimesService;
        private readonly IGeolocationService _geolocationService;
        private readonly ILogger<PrayerTimesController> _logger;

        public PrayerTimesController(
            IPrayerTimesService prayerTimesService,
            IGeolocationService geolocationService,
            ILogger<PrayerTimesController> logger)
        {
            _prayerTimesService = prayerTimesService;
            _geolocationService = geolocationService;
            _logger = logger;
        }

        // ---------- TODAY ----------
        /// <summary>
        /// Gets today's prayer times (GPS first)
        /// Priority: GPS coordinates → city/country query → profile claims
        /// </summary>
        /// <remarks>
        /// **Priority order:**
        /// 1. GPS coordinates from query (highest priority) - reverse geocoding is performed automatically
        /// 2. City/Country from query parameters (fallback)
        /// 3. City/Country from user JWT claims (last resort)
        /// 
        /// **Examples:**
        /// - By GPS: `/api/PrayerTimes/today?latitude=45.5416&longitude=10.2118` (Brescia)
        /// - By city: `/api/PrayerTimes/today?city=Brescia&country=Italy`
        /// - Using profile: `/api/PrayerTimes/today` (uses city/country from token)
        /// 
        /// **Calculation Methods:** See GetToday endpoint documentation for full list (default: 3 = MWL)
        /// 
        /// **School:** 0 = Standard (default), 1 = Hanafi
        /// </remarks>
        /// <param name="latitude">Latitude (-90 to 90). Takes highest priority. Example: 45.5416</param>
        /// <param name="longitude">Longitude (-180 to 180). Takes highest priority. Example: 10.2118</param>
        /// <param name="city">City name (fallback if coordinates not provided). Example: Brescia</param>
        /// <param name="country">Country name (fallback if coordinates not provided). Example: Italy</param>
        /// <param name="method">Calculation method (default: 3 = MWL)</param>
        /// <param name="school">School of thought (default: 0 = Standard)</param>
        /// <returns>Today's prayer times with source information</returns>
        /// <response code="200">Returns prayer times successfully</response>
        /// <response code="400">Invalid parameters (coordinates out of range, missing location)</response>
        /// <response code="502">AlAdhan API unavailable</response>
        [Authorize]
        [HttpGet("today")]
        [ProducesResponseType(typeof(PrayerTimesResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 502)]
        public async Task<IActionResult> Today(
            [FromQuery] double? latitude,
            [FromQuery] double? longitude,
            [FromQuery] string? city,
            [FromQuery] string? country,
            [FromQuery] int method = 3,
            [FromQuery] int school = 0,
            CancellationToken ct = default)
        {
            // 1) GPS first - sempre calcolo da coords + reverse geocoding server-side
            if (latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180)
            {
                _logger.LogInformation("Using GPS coordinates: lat={Latitude}, lng={Longitude}", latitude, longitude);

                // Reverse geocoding obbligatorio server-side per city/country/timezone
                GeocodingResult? place = null;
                try
                {
                    place = await _geolocationService.ReverseGeocodeAsync(
                        (decimal)latitude.Value, (decimal)longitude.Value, ct);
                    
                    if (place != null)
                    {
                        _logger.LogInformation("Reverse geocoding successful: lat={Lat}, lng={Lng} → city={City}, country={Country}",
                            latitude, longitude, place.City, place.Country);
                    }
                    else
                    {
                        _logger.LogWarning("Reverse geocoding returned null for lat={Lat}, lng={Lng}", latitude, longitude);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Reverse geocoding exception for {Lat},{Lng}", latitude, longitude);
                }

                // Se reverse fallisce o restituisce dati incompleti, prova city/country dai query params (se presenti)
                string? resolvedCity = place?.City;
                string? resolvedCountry = place?.Country;
                
                if (string.IsNullOrWhiteSpace(resolvedCity) || string.IsNullOrWhiteSpace(resolvedCountry))
                {
                    if (!string.IsNullOrWhiteSpace(city) && !string.IsNullOrWhiteSpace(country))
                    {
                        _logger.LogInformation("Using city/country from query params as fallback after reverse geocoding failure/incomplete data");
                        resolvedCity = city;
                        resolvedCountry = country;
                    }
                    else
                    {
                        _logger.LogWarning("Reverse geocoding failed/incomplete and no city/country in query params. Location fields will be empty, but prayer times are correct (calculated from coords).");
                    }
                }

                // Normalize timezone based on country (Italy → Europe/Rome, not Europe/Berlin)
                var tz = TzHelper.NormalizeTimezoneByCountry(place?.TimeZoneId, resolvedCountry ?? place?.Country);
                
                var raw = await _prayerTimesService.GetTodayByCoordsAsync(
                    latitude.Value, longitude.Value, method, school, tz, ct);

                // Guard-rail: Se AlAdhan down, ritorna 200 con location pieno e prayers:null
                if (raw == null)
                {
                    _logger.LogWarning("AlAdhan unavailable for lat={Lat}, lng={Lng}. Returning graceful response with location only.", latitude, longitude);
                    var gracefulResp = new PrayerTimesResponse(
                        Source: "coords",
                        Location: new LocationDto(
                            resolvedCity ?? string.Empty,
                            resolvedCountry ?? string.Empty,
                            tz), // Already normalized by country
                        Coords: new CoordsDto(
                            Math.Round(latitude.Value, 4),
                            Math.Round(longitude.Value, 4),
                            "p4"),
                        Method: method,
                        School: school,
                        Date: DateTime.UtcNow.ToString("yyyy-MM-dd"),
                        Prayers: null, // Upstream unavailable
                        NextPrayerName: null,
                        NextPrayerTime: null,
                        NextFajrTime: null,
                        Error: "UPSTREAM_UNAVAILABLE");
                    return Ok(gracefulResp);
                }

                var resp = new PrayerTimesResponse(
                    Source: "coords",
                    Location: new LocationDto(
                        resolvedCity ?? string.Empty,
                        resolvedCountry ?? string.Empty,
                        tz), // Already normalized by country
                    Coords: new CoordsDto(
                        Math.Round(latitude.Value, 4),
                        Math.Round(longitude.Value, 4),
                        "p4"),
                    Method: method,
                    School: school,
                    Date: raw.Date,
                    Prayers: new PrayersDto(
                        raw.Prayers.Fajr,
                        raw.Prayers.Sunrise,
                        raw.Prayers.Dhuhr,
                        raw.Prayers.Asr,
                        raw.Prayers.Maghrib,
                        raw.Prayers.Isha,
                        raw.Prayers.Imsak,
                        raw.Prayers.Midnight),
                    NextPrayerName: raw.NextPrayerName,
                    NextPrayerTime: raw.NextPrayerTime,
                    NextFajrTime: raw.NextFajrTime);

                return Ok(resp);
            }

            // 2) Fallback: city/country dai query param
            if (!string.IsNullOrWhiteSpace(city) && !string.IsNullOrWhiteSpace(country))
            {
                _logger.LogInformation("Using city/country from query: {City}, {Country}", city, country);

                var raw = await _prayerTimesService.GetTodayByCityAsync(
                    city!, country!, method, school, null, ct);

                if (raw == null)
                {
                    return Problem(
                        statusCode: 502,
                        title: "Service unavailable",
                        detail: "AlAdhan API is temporarily unavailable.");
                }

                var resp = new PrayerTimesResponse(
                    Source: "fallback_city_country",
                    Location: new LocationDto(city, country, TzHelper.ToIana(raw.Timezone)),
                    Coords: new CoordsDto(null, null, null),
                    Method: method,
                    School: school,
                    Date: raw.Date,
                    Prayers: new PrayersDto(
                        raw.Prayers.Fajr,
                        raw.Prayers.Sunrise,
                        raw.Prayers.Dhuhr,
                        raw.Prayers.Asr,
                        raw.Prayers.Maghrib,
                        raw.Prayers.Isha,
                        raw.Prayers.Imsak,
                        raw.Prayers.Midnight),
                    NextPrayerName: raw.NextPrayerName,
                    NextPrayerTime: raw.NextPrayerTime,
                    NextFajrTime: raw.NextFajrTime);

                return Ok(resp);
            }

            // 3) Ultimo fallback: claims profilo (SOLO se proprio serve)
            var claimCity = User.FindFirst("city")?.Value;
            var claimCountry = User.FindFirst("country")?.Value;
            if (!string.IsNullOrWhiteSpace(claimCity) && !string.IsNullOrWhiteSpace(claimCountry))
            {
                _logger.LogInformation("Using city/country from profile: {City}, {Country}", claimCity, claimCountry);

                var raw = await _prayerTimesService.GetTodayByCityAsync(
                    claimCity!, claimCountry!, method, school, null, ct);

                if (raw == null)
                {
                    return Problem(
                        statusCode: 502,
                        title: "Service unavailable",
                        detail: "AlAdhan API is temporarily unavailable.");
                }

                var resp = new PrayerTimesResponse(
                    Source: "fallback_profile",
                    Location: new LocationDto(claimCity, claimCountry, TzHelper.ToIana(raw.Timezone)),
                    Coords: new CoordsDto(null, null, null),
                    Method: method,
                    School: school,
                    Date: raw.Date,
                    Prayers: new PrayersDto(
                        raw.Prayers.Fajr,
                        raw.Prayers.Sunrise,
                        raw.Prayers.Dhuhr,
                        raw.Prayers.Asr,
                        raw.Prayers.Maghrib,
                        raw.Prayers.Isha,
                        raw.Prayers.Imsak,
                        raw.Prayers.Midnight),
                    NextPrayerName: raw.NextPrayerName,
                    NextPrayerTime: raw.NextPrayerTime,
                    NextFajrTime: raw.NextFajrTime);

                return Ok(resp);
            }

            return Problem(
                statusCode: 400,
                title: "Invalid parameters",
                detail: "Provide (latitude, longitude) or (city, country). GPS is the primary source.");
        }

        // ---------- WEEK / INTERVAL ----------
        /// <summary>
        /// Gets prayer times for a date range (GPS first)
        /// Priority: GPS coordinates → city/country query → profile claims
        /// </summary>
        /// <remarks>
        /// **Priority order:** Same as Today endpoint
        /// 
        /// **Examples:**
        /// - By GPS: `/api/PrayerTimes/week?latitude=45.5416&longitude=10.2118&days=7`
        /// - By city: `/api/PrayerTimes/week?city=Brescia&country=Italy&days=14`
        /// - Custom start: `/api/PrayerTimes/week?start=2024-01-01&days=7`
        /// </remarks>
        /// <param name="latitude">Latitude (-90 to 90). Takes highest priority.</param>
        /// <param name="longitude">Longitude (-180 to 180). Takes highest priority.</param>
        /// <param name="city">City name (fallback if coordinates not provided).</param>
        /// <param name="country">Country name (fallback if coordinates not provided).</param>
        /// <param name="method">Calculation method (default: 3 = MWL)</param>
        /// <param name="school">School of thought (default: 0 = Standard)</param>
        /// <param name="start">Start date in yyyy-MM-dd format (optional)</param>
        /// <param name="offset">Days offset from today (default: 0). Ignored if start is provided.</param>
        /// <param name="days">Number of days to fetch (1-14, default: 7)</param>
        /// <returns>Prayer times for the requested date range</returns>
        /// <response code="200">Returns prayer times successfully</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="502">AlAdhan API unavailable</response>
        [Authorize]
        [HttpGet("week")]
        [ProducesResponseType(typeof(PrayerWeekResponseV2), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 502)]
        public async Task<IActionResult> Week(
            [FromQuery] double? latitude,
            [FromQuery] double? longitude,
            [FromQuery] string? city,
            [FromQuery] string? country,
            [FromQuery] int method = 3,
            [FromQuery] int school = 0,
            [FromQuery] string? start = null,
            [FromQuery] int offset = 0,
            [FromQuery] int days = 7,
            CancellationToken ct = default)
        {
            // Validate days
            if (days < 1 || days > 14)
            {
                return Problem(
                    statusCode: 400,
                    title: "Invalid days parameter",
                    detail: "Days must be between 1 and 14.");
            }

            // Calculate start date
            DateTime? startDate = null;
            if (!string.IsNullOrWhiteSpace(start) && DateTime.TryParse(start, out var parsed))
                startDate = parsed.Date;
            else if (offset != 0)
                startDate = DateTime.UtcNow.Date.AddDays(offset);

            // 1) GPS first - sempre calcolo da coords + reverse geocoding server-side
            if (latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180)
            {
                _logger.LogInformation("Using GPS coordinates: lat={Latitude}, lng={Longitude}", latitude, longitude);

                // Reverse geocoding obbligatorio server-side per city/country/timezone
                GeocodingResult? place = null;
                try
                {
                    place = await _geolocationService.ReverseGeocodeAsync(
                        (decimal)latitude.Value, (decimal)longitude.Value, ct);
                    
                    if (place != null)
                    {
                        _logger.LogInformation("Reverse geocoding successful: lat={Lat}, lng={Lng} → city={City}, country={Country}",
                            latitude, longitude, place.City, place.Country);
                    }
                    else
                    {
                        _logger.LogWarning("Reverse geocoding returned null for lat={Lat}, lng={Lng}", latitude, longitude);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Reverse geocoding exception for {Lat},{Lng}", latitude, longitude);
                }

                // Se reverse fallisce o restituisce dati incompleti, prova city/country dai query params (se presenti)
                string? resolvedCity = place?.City;
                string? resolvedCountry = place?.Country;
                
                if (string.IsNullOrWhiteSpace(resolvedCity) || string.IsNullOrWhiteSpace(resolvedCountry))
                {
                    if (!string.IsNullOrWhiteSpace(city) && !string.IsNullOrWhiteSpace(country))
                    {
                        _logger.LogInformation("Using city/country from query params as fallback after reverse geocoding failure/incomplete data");
                        resolvedCity = city;
                        resolvedCountry = country;
                    }
                    else
                    {
                        _logger.LogWarning("Reverse geocoding failed/incomplete and no city/country in query params. Location fields will be empty, but prayer times are correct (calculated from coords).");
                    }
                }

                // Normalize timezone based on country (Italy → Europe/Rome, not Europe/Berlin)
                var tz = TzHelper.NormalizeTimezoneByCountry(place?.TimeZoneId, resolvedCountry ?? place?.Country);
                
                var raw = await _prayerTimesService.GetWeekByCoordsAsync(
                    latitude.Value, longitude.Value, method, school, startDate, days, tz, ct);

                // Guard-rail: Se AlAdhan down, ritorna 200 con location pieno e days:[] + error
                if (raw == null)
                {
                    _logger.LogWarning("AlAdhan unavailable for lat={Lat}, lng={Lng}. Returning graceful response with location only.", latitude, longitude);
                    var gracefulResp = new PrayerWeekResponseV2(
                        Source: "coords",
                        Location: new LocationDto(
                            resolvedCity ?? string.Empty,
                            resolvedCountry ?? string.Empty,
                            tz), // Already normalized by country
                        Coords: new CoordsDto(
                            Math.Round(latitude.Value, 4),
                            Math.Round(longitude.Value, 4),
                            "p4"),
                        Method: method,
                        School: school,
                        RangeStart: startDate?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.ToString("yyyy-MM-dd"),
                        RangeEnd: (startDate?.AddDays(days - 1) ?? DateTime.UtcNow.AddDays(days - 1)).ToString("yyyy-MM-dd"),
                        Days: new List<PrayerDayDto>(), // Empty when upstream unavailable
                        Error: "UPSTREAM_UNAVAILABLE");
                    return Ok(gracefulResp);
                }

                var resp = new PrayerWeekResponseV2(
                    Source: "coords",
                    Location: new LocationDto(
                        resolvedCity ?? string.Empty,
                        resolvedCountry ?? string.Empty,
                        tz), // Already normalized by country
                    Coords: new CoordsDto(
                        Math.Round(latitude.Value, 4),
                        Math.Round(longitude.Value, 4),
                        "p4"),
                    Method: method,
                    School: school,
                    RangeStart: raw.RangeStart,
                    RangeEnd: raw.RangeEnd,
                    Days: raw.Days);

                return Ok(resp);
            }

            // 2) Fallback: city/country dai query param
            if (!string.IsNullOrWhiteSpace(city) && !string.IsNullOrWhiteSpace(country))
            {
                _logger.LogInformation("Using city/country from query: {City}, {Country}", city, country);

                var raw = await _prayerTimesService.GetWeekByCityAsync(
                    city!, country!, method, school, startDate, days, null, ct);

                if (raw == null)
                {
                    return Problem(
                        statusCode: 502,
                        title: "Service unavailable",
                        detail: "AlAdhan API is temporarily unavailable.");
                }

                var resp = new PrayerWeekResponseV2(
                    Source: "fallback_city_country",
                    Location: new LocationDto(
                        city,
                        country,
                        TzHelper.ToIana(raw.Days.FirstOrDefault()?.Timezone)),
                    Coords: new CoordsDto(null, null, null),
                    Method: method,
                    School: school,
                    RangeStart: raw.RangeStart,
                    RangeEnd: raw.RangeEnd,
                    Days: raw.Days);

                return Ok(resp);
            }

            // 3) Ultimo fallback: claims profilo
            var claimCity = User.FindFirst("city")?.Value;
            var claimCountry = User.FindFirst("country")?.Value;
            if (!string.IsNullOrWhiteSpace(claimCity) && !string.IsNullOrWhiteSpace(claimCountry))
            {
                _logger.LogInformation("Using city/country from profile: {City}, {Country}", claimCity, claimCountry);

                var raw = await _prayerTimesService.GetWeekByCityAsync(
                    claimCity!, claimCountry!, method, school, startDate, days, null, ct);

                if (raw == null)
                {
                    return Problem(
                        statusCode: 502,
                        title: "Service unavailable",
                        detail: "AlAdhan API is temporarily unavailable.");
                }

                var resp = new PrayerWeekResponseV2(
                    Source: "fallback_profile",
                    Location: new LocationDto(
                        claimCity,
                        claimCountry,
                        TzHelper.ToIana(raw.Days.FirstOrDefault()?.Timezone)),
                    Coords: new CoordsDto(null, null, null),
                    Method: method,
                    School: school,
                    RangeStart: raw.RangeStart,
                    RangeEnd: raw.RangeEnd,
                    Days: raw.Days);

                return Ok(resp);
            }

            return Problem(
                statusCode: 400,
                title: "Invalid parameters",
                detail: "Provide (latitude, longitude) or (city, country). GPS is the primary source.");
        }
    }
}
