using System.Globalization;
using System.Net.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using FajrSquad.Core.DTOs;
using FajrSquad.Infrastructure.Services;

namespace FajrSquad.Infrastructure.Services.PrayerTimes
{
    /// <summary>
    /// Implementation of prayer times service using AlAdhan API
    /// Isolated to PrayerTimes feature only
    /// </summary>
    public class PrayerTimesService : IPrayerTimesService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PrayerTimesService> _logger;
        private readonly ICacheService _cacheService;
        private readonly IGeolocationService _geolocationService;
        private const string AladhanBaseUrl = "https://api.aladhan.com/v1";
        private const int CacheMinutes = 30; // Cache prayer times for 30 minutes

        public PrayerTimesService(
            IHttpClientFactory httpClientFactory,
            ILogger<PrayerTimesService> logger,
            ICacheService cacheService,
            IGeolocationService geolocationService)
        {
            try
            {
                _httpClient = httpClientFactory.CreateClient("PrayerTimes");
            }
            catch (Exception ex)
            {
                // Fallback to default client if named client fails
                logger.LogWarning(ex, "Failed to create named HttpClient 'PrayerTimes', using default client");
                _httpClient = httpClientFactory.CreateClient();
            }
            
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
            _logger = logger;
            _cacheService = cacheService;
            _geolocationService = geolocationService;
        }

        // GPS-first methods
        public async Task<PrayerTodayResponse?> GetTodayByCoordsAsync(
            double latitude,
            double longitude,
            int method = 3,
            int school = 0,
            string? timeZoneId = null,
            CancellationToken cancellationToken = default)
        {
            return await GetTodayByCoordinatesAsync((decimal)latitude, (decimal)longitude, timeZoneId, method, school, cancellationToken);
        }

        public async Task<PrayerTodayResponse?> GetTodayByCityAsync(
            string city,
            string country,
            int method = 3,
            int school = 0,
            string? timeZoneId = null,
            CancellationToken cancellationToken = default)
        {
            return await GetTodayByCityInternalAsync(city, country, timeZoneId, method, school, cancellationToken);
        }

        public async Task<PrayerWeekResponse?> GetWeekByCoordsAsync(
            double latitude,
            double longitude,
            int method = 3,
            int school = 0,
            DateTime? startDate = null,
            int days = 7,
            string? timeZoneId = null,
            CancellationToken cancellationToken = default)
        {
            return await GetWeekByCoordinatesAsync((decimal)latitude, (decimal)longitude, timeZoneId, method, school, startDate ?? DateTime.UtcNow.Date, days, cancellationToken);
        }

        public async Task<PrayerWeekResponse?> GetWeekByCityAsync(
            string city,
            string country,
            int method = 3,
            int school = 0,
            DateTime? startDate = null,
            int days = 7,
            string? timeZoneId = null,
            CancellationToken cancellationToken = default)
        {
            return await GetWeekByCityInternalAsync(city, country, timeZoneId, method, school, startDate ?? DateTime.UtcNow.Date, days, cancellationToken);
        }

        // Legacy methods (kept for backward compatibility)
        public async Task<PrayerTodayResponse?> GetTodayPrayerTimesAsync(
            decimal? latitude = null,
            decimal? longitude = null,
            string? city = null,
            string? country = null,
            string? timeZoneId = null,
            int method = 3,
            int school = 0,
            CancellationToken cancellationToken = default)
        {
            string? resolvedCity = city;
            string? resolvedCountry = country;
            string? resolvedTimeZone = timeZoneId;

            // If coordinates provided, try to use them first
            if (latitude.HasValue && longitude.HasValue)
            {
                if (!_geolocationService.IsValidCoordinates(latitude.Value, longitude.Value))
                {
                    _logger.LogWarning("Invalid coordinates provided: lat={Latitude}, lng={Longitude}", latitude, longitude);
                    return null;
                }

                // Try reverse geocoding if city/country not provided
                if (string.IsNullOrWhiteSpace(resolvedCity) || string.IsNullOrWhiteSpace(resolvedCountry))
                {
                    var geoResult = await _geolocationService.ReverseGeocodeAsync(
                        latitude.Value, longitude.Value, cancellationToken);
                    if (geoResult != null)
                    {
                        resolvedCity = geoResult.City;
                        resolvedCountry = geoResult.Country;
                        if (string.IsNullOrWhiteSpace(resolvedTimeZone))
                            resolvedTimeZone = geoResult.TimeZoneId;
                    }
                }

                // Build cache key with coordinates
                var latRounded = Math.Round(latitude.Value, 4);
                var lngRounded = Math.Round(longitude.Value, 4);
                var localDate = resolvedTimeZone != null
                    ? GetLocalDate(resolvedTimeZone)
                    : DateTime.UtcNow.Date;
                var geoKey = $"{latRounded:F4}:{lngRounded:F4}";
                var cacheKey = $"pt:today:{geoKey}:{method}:{school}:{localDate:yyyy-MM-dd}@{resolvedTimeZone ?? "UTC"}";

                // Check cache
                var cached = await _cacheService.GetAsync<PrayerTodayResponse>(cacheKey);
                if (cached != null)
                {
                    _logger.LogDebug("Prayer times cache hit for today (coords)");
                    return cached;
                }

                // Fetch by coordinates
                var result = await GetTodayByCoordinatesAsync(
                    latitude.Value, longitude.Value, resolvedTimeZone, method, school, cancellationToken);
                if (result != null)
                {
                    result.City = resolvedCity ?? result.City;
                    result.Country = resolvedCountry ?? result.Country;
                    // Normalize timezone to IANA before caching
                    if (!string.IsNullOrWhiteSpace(resolvedTimeZone))
                        result.Timezone = TzHelper.NormalizeTimezoneByCountry(resolvedTimeZone, resolvedCountry);
                    else if (!string.IsNullOrWhiteSpace(result.Timezone))
                        result.Timezone = TzHelper.NormalizeTimezoneByCountry(result.Timezone, result.Country);
                    
                    await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(CacheMinutes));
                    return result;
                }
            }

            // Fallback to city/country if coordinates failed or not provided
            if (string.IsNullOrWhiteSpace(resolvedCity))
            {
                _logger.LogError("City is required for prayer times calculation when coordinates are not provided or failed.");
                throw new ArgumentException("City is required for prayer times calculation.");
            }

            resolvedCountry ??= "Italy";

            // Build cache key with city/country
            var localDateCity = resolvedTimeZone != null
                ? GetLocalDate(resolvedTimeZone)
                : DateTime.UtcNow.Date;
            var cityKey = $"{Uri.EscapeDataString(resolvedCity)}:{Uri.EscapeDataString(resolvedCountry)}";
            var cacheKeyCity = $"pt:today:{cityKey}:{method}:{school}:{localDateCity:yyyy-MM-dd}@{resolvedTimeZone ?? "UTC"}";

            // Check cache
            var cachedCity = await _cacheService.GetAsync<PrayerTodayResponse>(cacheKeyCity);
            if (cachedCity != null)
            {
                _logger.LogDebug("Prayer times cache hit for today (city)");
                return cachedCity;
            }

            var resultCity = await GetTodayByCityInternalAsync(
                resolvedCity, resolvedCountry, resolvedTimeZone, method, school, cancellationToken);
            if (resultCity != null)
            {
                await _cacheService.SetAsync(cacheKeyCity, resultCity, TimeSpan.FromMinutes(CacheMinutes));
            }

            return resultCity;
        }

        public async Task<PrayerWeekResponse?> GetWeekPrayerTimesAsync(
            decimal? latitude = null,
            decimal? longitude = null,
            string? city = null,
            string? country = null,
            string? timeZoneId = null,
            int method = 3,
            int school = 0,
            DateTime? startDate = null,
            int days = 7,
            CancellationToken cancellationToken = default)
        {
            string? resolvedCity = city;
            string? resolvedCountry = country;
            string? resolvedTimeZone = timeZoneId;

            // If coordinates provided, try to use them first
            if (latitude.HasValue && longitude.HasValue)
            {
                if (!_geolocationService.IsValidCoordinates(latitude.Value, longitude.Value))
                {
                    _logger.LogWarning("Invalid coordinates provided: lat={Latitude}, lng={Longitude}", latitude, longitude);
                    return null;
                }

                // Try reverse geocoding if city/country not provided
                if (string.IsNullOrWhiteSpace(resolvedCity) || string.IsNullOrWhiteSpace(resolvedCountry))
                {
                    var geoResult = await _geolocationService.ReverseGeocodeAsync(
                        latitude.Value, longitude.Value, cancellationToken);
                    if (geoResult != null)
                    {
                        resolvedCity = geoResult.City;
                        resolvedCountry = geoResult.Country;
                        if (string.IsNullOrWhiteSpace(resolvedTimeZone))
                            resolvedTimeZone = geoResult.TimeZoneId;
                    }
                }

                // Build cache key with coordinates
                var latRounded = Math.Round(latitude.Value, 4);
                var lngRounded = Math.Round(longitude.Value, 4);
                var start = startDate ?? DateTime.UtcNow.Date;
                var localStart = resolvedTimeZone != null
                    ? TimeZoneInfo.ConvertTimeFromUtc(start, TimeZoneInfo.FindSystemTimeZoneById(resolvedTimeZone))
                    : start;
                var geoKey = $"{latRounded:F4}:{lngRounded:F4}";
                var cacheKey = $"pt:week:{geoKey}:{method}:{school}:{localStart:yyyy-MM-dd}@{resolvedTimeZone ?? "UTC"}:{days}";

                // Check cache
                var cached = await _cacheService.GetAsync<PrayerWeekResponse>(cacheKey);
                if (cached != null)
                {
                    _logger.LogDebug("Prayer times cache hit for week (coords)");
                    return cached;
                }

                // Fetch by coordinates
                var result = await GetWeekByCoordinatesAsync(
                    latitude.Value, longitude.Value, resolvedTimeZone, method, school, start, days, cancellationToken);
                if (result != null)
                {
                    result.City = resolvedCity ?? result.City;
                    result.Country = resolvedCountry ?? result.Country;
                    
                    // Normalize timezone to IANA before caching (for all days)
                    if (result.Days != null && result.Days.Count > 0)
                    {
                        var normalizedTz = TzHelper.NormalizeTimezoneByCountry(resolvedTimeZone, resolvedCountry ?? result.Country);
                        foreach (var day in result.Days)
                        {
                            if (!string.IsNullOrWhiteSpace(day.Timezone))
                                day.Timezone = TzHelper.NormalizeTimezoneByCountry(day.Timezone, resolvedCountry ?? result.Country);
                            else
                                day.Timezone = normalizedTz;
                        }
                    }
                    
                    await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(CacheMinutes));
                    return result;
                }
            }

            // Fallback to city/country if coordinates failed or not provided
            if (string.IsNullOrWhiteSpace(resolvedCity))
            {
                _logger.LogError("City is required for prayer times calculation when coordinates are not provided or failed.");
                throw new ArgumentException("City is required for prayer times calculation.");
            }

            resolvedCountry ??= "Italy";

            // Build cache key with city/country
            var startCity = startDate ?? DateTime.UtcNow.Date;
            var localStartCity = resolvedTimeZone != null
                ? TimeZoneInfo.ConvertTimeFromUtc(startCity, TimeZoneInfo.FindSystemTimeZoneById(resolvedTimeZone))
                : startCity;
            var cityKey = $"{Uri.EscapeDataString(resolvedCity)}:{Uri.EscapeDataString(resolvedCountry)}";
            var cacheKeyCity = $"pt:week:{cityKey}:{method}:{school}:{localStartCity:yyyy-MM-dd}@{resolvedTimeZone ?? "UTC"}:{days}";

            // Check cache
            var cachedCity = await _cacheService.GetAsync<PrayerWeekResponse>(cacheKeyCity);
            if (cachedCity != null)
            {
                _logger.LogDebug("Prayer times cache hit for week (city)");
                return cachedCity;
            }

            var resultCity = await GetWeekByCityInternalAsync(
                resolvedCity, resolvedCountry, resolvedTimeZone, method, school, startCity, days, cancellationToken);
            if (resultCity != null)
            {
                await _cacheService.SetAsync(cacheKeyCity, resultCity, TimeSpan.FromMinutes(CacheMinutes));
            }

            return resultCity;
        }

        private DateTime GetLocalDate(string timeZoneId)
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
            }
            catch
            {
                return DateTime.UtcNow.Date;
            }
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

        private async Task<PrayerTodayResponse?> GetTodayByCoordinatesAsync(
            decimal latitude,
            decimal longitude,
            string? timeZoneId,
            int method,
            int school,
            CancellationToken cancellationToken)
        {
            try
            {
                // Convert timezone to IANA format (AlAdhan requires IANA, not Windows)
                var ianaTz = TzHelper.ToIana(timeZoneId);

                // Use /v1/timings (without date in path) - more robust
                var queryParams = new Dictionary<string, string?>
                {
                    ["latitude"] = latitude.ToString(CultureInfo.InvariantCulture),
                    ["longitude"] = longitude.ToString(CultureInfo.InvariantCulture),
                    ["method"] = method.ToString(),
                    ["school"] = school.ToString(),
                    ["timezonestring"] = ianaTz
                };

                var url = QueryHelpers.AddQueryString($"{AladhanBaseUrl}/timings", queryParams);

                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                var response = await _httpClient.SendAsync(req, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("AlAdhan timings BAD: {StatusCode} {Body}", response.StatusCode, body);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var json = JObject.Parse(content);
                var data = json["data"];
                if (data == null) return null;

                // Always use IANA timezone in response
                var tzFromApi = data["meta"]?["timezone"]?.ToString();
                var tz = !string.IsNullOrWhiteSpace(tzFromApi) 
                    ? TzHelper.ToIana(tzFromApi) 
                    : TzHelper.ToIana(timeZoneId);
                var timings = MapTimings(data["timings"]!);

                // Calculate next prayer
                var (nextName, nextTime) = CalculateNextPrayer(data["timings"]!, tz);

                // Get tomorrow's Fajr (pass IANA timezone)
                var nextFajr = await GetTomorrowFajrByCoordinatesAsync(latitude, longitude, ianaTz, method, school, cancellationToken);

                return new PrayerTodayResponse
                {
                    City = "", // Will be filled by caller
                    Country = "", // Will be filled by caller
                    Date = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
                    Timezone = tz,
                    Prayers = timings,
                    NextPrayerName = nextName,
                    NextPrayerTime = nextTime,
                    NextFajrTime = nextFajr
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching today prayer times by coordinates");
                return null;
            }
        }

        private async Task<PrayerTodayResponse?> GetTodayByCityInternalAsync(
            string city,
            string country,
            string? timeZoneId,
            int method,
            int school,
            CancellationToken cancellationToken)
        {
            try
            {
                var queryParams = new Dictionary<string, string?>
                {
                    ["city"] = city,
                    ["country"] = country,
                    ["method"] = method.ToString(),
                    ["school"] = school.ToString()
                };

                if (!string.IsNullOrWhiteSpace(timeZoneId))
                    queryParams["timezonestring"] = timeZoneId;

                var url = QueryHelpers.AddQueryString($"{AladhanBaseUrl}/timingsByCity", queryParams);

                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                var response = await _httpClient.SendAsync(req, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("AlAdhan timingsByCity BAD: {StatusCode} {Body}", response.StatusCode, body);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var json = JObject.Parse(content);
                var data = json["data"];
                if (data == null) return null;

                var tz = data["meta"]?["timezone"]?.ToString() ?? timeZoneId ?? "UTC";
                var timings = MapTimings(data["timings"]!);

                // Calculate next prayer
                var (nextName, nextTime) = CalculateNextPrayer(data["timings"]!, tz);

                // Get tomorrow's Fajr
                var nextFajr = await GetTomorrowFajrByCityAsync(city, country, timeZoneId, method, school, cancellationToken);

                return new PrayerTodayResponse
                {
                    City = city,
                    Country = country,
                    Date = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
                    Timezone = tz,
                    Prayers = timings,
                    NextPrayerName = nextName,
                    NextPrayerTime = nextTime,
                    NextFajrTime = nextFajr
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching today prayer times by city");
                return null;
            }
        }

        private async Task<PrayerWeekResponse?> GetWeekByCoordinatesAsync(
            decimal latitude,
            decimal longitude,
            string? timeZoneId,
            int method,
            int school,
            DateTime startDate,
            int days,
            CancellationToken cancellationToken)
        {
            try
            {
                days = Math.Clamp(days, 1, 14);
                
                // Convert timezone to IANA format (AlAdhan requires IANA, not Windows)
                var ianaTz = TzHelper.ToIana(timeZoneId);
                
                // Calculate date range in the target timezone
                TimeZoneInfo? tzInfo = null;
                try
                {
                    tzInfo = TimeZoneInfo.FindSystemTimeZoneById(ianaTz);
                }
                catch
                {
                    // If timezone not found, use UTC
                    tzInfo = TimeZoneInfo.Utc;
                }

                var start = startDate.Date;
                var end = start.AddDays(days - 1);

                // Get distinct months needed (week might span across months)
                var monthsToFetch = new HashSet<(int y, int m)>();
                var cursor = start;
                for (int i = 0; i < days; i++)
                {
                    monthsToFetch.Add((cursor.Year, cursor.Month));
                    cursor = cursor.AddDays(1);
                }

                var monthCache = new Dictionary<(int y, int m), JArray>();
                foreach (var (y, m) in monthsToFetch)
                {
                    var queryParams = new Dictionary<string, string?>
                    {
                        ["latitude"] = latitude.ToString(CultureInfo.InvariantCulture),
                        ["longitude"] = longitude.ToString(CultureInfo.InvariantCulture),
                        ["method"] = method.ToString(),
                        ["school"] = school.ToString(),
                        ["month"] = m.ToString(),
                        ["year"] = y.ToString(),
                        ["timezonestring"] = ianaTz
                    };

                    var url = QueryHelpers.AddQueryString($"{AladhanBaseUrl}/calendar", queryParams);

                    using var req = new HttpRequestMessage(HttpMethod.Get, url);
                    var response = await _httpClient.SendAsync(req, cancellationToken);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogWarning("AlAdhan calendar BAD: {Status} {Body} for {Year}/{Month}", 
                            response.StatusCode, body, y, m);
                        continue;
                    }

                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var json = JObject.Parse(content);
                    monthCache[(y, m)] = (JArray?)json["data"] ?? new JArray();
                }

                // Collect all days from all months, then filter by date range
                var allDays = new List<(DateTime date, JToken dayData)>();
                foreach (var (y, m) in monthsToFetch)
                {
                    if (!monthCache.TryGetValue((y, m), out var arr)) continue;
                    
                    for (int day = 1; day <= DateTime.DaysInMonth(y, m); day++)
                    {
                        var date = new DateTime(y, m, day);
                        var idx = day - 1;
                        if (idx < 0 || idx >= arr.Count) continue;
                        
                        var dayData = arr[idx];
                        allDays.Add((date, dayData));
                    }
                }

                // Filter to exact date range and build response
                var daysOut = new List<PrayerDayDto>(days);
                var it = start;
                for (int i = 0; i < days; i++, it = it.AddDays(1))
                {
                    var dayData = allDays.FirstOrDefault(d => d.date.Date == it.Date);
                    if (dayData.dayData == null) continue;

                    var d = dayData.dayData;
                    var timings = d["timings"]!;
                    var hijri = d["date"]?["hijri"]?["date"]?.ToString() ?? "";
                    var greg = d["date"]?["gregorian"]?["date"]?.ToString() ?? "";
                    
                    // Always use the normalized IANA timezone (from country-based normalization)
                    // Don't trust API timezone which might be Europe/Berlin for Italy
                    var tz = ianaTz;

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

                return new PrayerWeekResponse
                {
                    City = "", // Will be filled by caller
                    Country = "", // Will be filled by caller
                    Method = method,
                    School = school,
                    RangeStart = start.ToString("yyyy-MM-dd"),
                    RangeEnd = end.ToString("yyyy-MM-dd"),
                    Days = daysOut
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching week prayer times by coordinates");
                return null;
            }
        }

        private async Task<PrayerWeekResponse?> GetWeekByCityInternalAsync(
            string city,
            string country,
            string? timeZoneId,
            int method,
            int school,
            DateTime startDate,
            int days,
            CancellationToken cancellationToken)
        {
            try
            {
                days = Math.Clamp(days, 1, 14);
                
                // Convert timezone to IANA format (AlAdhan requires IANA, not Windows)
                var ianaTz = TzHelper.ToIana(timeZoneId);
                
                var monthsToFetch = new HashSet<(int y, int m)>();
                var cursor = startDate;
                for (int i = 0; i < days; i++)
                {
                    monthsToFetch.Add((cursor.Year, cursor.Month));
                    cursor = cursor.AddDays(1);
                }

                var monthCache = new Dictionary<(int y, int m), JArray>();
                foreach (var (y, m) in monthsToFetch)
                {
                    var queryParams = new Dictionary<string, string?>
                    {
                        ["city"] = city,
                        ["country"] = country,
                        ["method"] = method.ToString(),
                        ["school"] = school.ToString(),
                        ["month"] = m.ToString(),
                        ["year"] = y.ToString(),
                        ["timezonestring"] = ianaTz
                    };

                    var url = QueryHelpers.AddQueryString($"{AladhanBaseUrl}/calendarByCity", queryParams);

                    using var req = new HttpRequestMessage(HttpMethod.Get, url);
                    var response = await _httpClient.SendAsync(req, cancellationToken);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogWarning("AlAdhan calendarByCity BAD: {Status} {Body} for {Year}/{Month}", 
                            response.StatusCode, body, y, m);
                        continue;
                    }

                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var json = JObject.Parse(content);
                    monthCache[(y, m)] = (JArray?)json["data"] ?? new JArray();
                }

                // Collect all days from all months, then filter by date range
                var allDays = new List<(DateTime date, JToken dayData)>();
                foreach (var (y, m) in monthsToFetch)
                {
                    if (!monthCache.TryGetValue((y, m), out var arr)) continue;
                    
                    for (int day = 1; day <= DateTime.DaysInMonth(y, m); day++)
                    {
                        var date = new DateTime(y, m, day);
                        var idx = day - 1;
                        if (idx < 0 || idx >= arr.Count) continue;
                        
                        var dayData = arr[idx];
                        allDays.Add((date, dayData));
                    }
                }

                // Filter to exact date range and build response
                var start = startDate.Date;
                var end = start.AddDays(days - 1);
                var daysOut = new List<PrayerDayDto>(days);
                var it = start;
                for (int i = 0; i < days; i++, it = it.AddDays(1))
                {
                    var dayData = allDays.FirstOrDefault(d => d.date.Date == it.Date);
                    if (dayData.dayData == null) continue;

                    var d = dayData.dayData;
                    var timings = d["timings"]!;
                    var hijri = d["date"]?["hijri"]?["date"]?.ToString() ?? "";
                    var greg = d["date"]?["gregorian"]?["date"]?.ToString() ?? "";
                    
                    // Always use the normalized IANA timezone (from country-based normalization)
                    // Don't trust API timezone which might be Europe/Berlin for Italy
                    var tz = ianaTz;

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

                return new PrayerWeekResponse
                {
                    City = city,
                    Country = country,
                    Method = method,
                    School = school,
                    RangeStart = start.ToString("yyyy-MM-dd"),
                    RangeEnd = end.ToString("yyyy-MM-dd"),
                    Days = daysOut
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching week prayer times by city");
                return null;
            }
        }

        private (string? name, string? time) CalculateNextPrayer(JToken timings, string timeZoneId)
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

                var names = new[] { "Fajr", "Dhuhr", "Asr", "Maghrib", "Isha" };
                
                // Try today's prayers first
                foreach (var name in names)
                {
                    var timeStr = timings[name]?.ToString();
                    if (string.IsNullOrWhiteSpace(timeStr)) continue;

                    // Remove timezone suffix if present (e.g., "18:19 (CET)" → "18:19")
                    timeStr = timeStr.Split(' ')[0];

                    var parts = timeStr.Split(':');
                    if (parts.Length < 2) continue;

                    if (!int.TryParse(parts[0], out var hour) || !int.TryParse(parts[1], out var minute))
                        continue;

                    var prayerTime = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0, DateTimeKind.Unspecified);
                    if (prayerTime > now)
                    {
                        return (name, prayerTime.ToString("HH:mm"));
                    }
                }

                // If no prayer found today (after Isha), next prayer is tomorrow's Fajr
                // This is handled by NextFajrTime in the response
                return (null, null);
            }
            catch
            {
                // Safe fallback
            }

            return (null, null);
        }

        private async Task<string?> GetTomorrowFajrByCoordinatesAsync(
            decimal latitude,
            decimal longitude,
            string? timeZoneId,
            int method,
            int school,
            CancellationToken cancellationToken)
        {
            try
            {
                // Use /v1/timings with date parameter instead of path segment
                var tomorrow = DateTime.UtcNow.Date.AddDays(1);
                var queryParams = new Dictionary<string, string?>
                {
                    ["latitude"] = latitude.ToString(CultureInfo.InvariantCulture),
                    ["longitude"] = longitude.ToString(CultureInfo.InvariantCulture),
                    ["method"] = method.ToString(),
                    ["school"] = school.ToString(),
                    ["date"] = tomorrow.ToString("dd-MM-yyyy")
                };

                if (!string.IsNullOrWhiteSpace(timeZoneId))
                    queryParams["timezonestring"] = timeZoneId;

                var url = QueryHelpers.AddQueryString($"{AladhanBaseUrl}/timings", queryParams);

                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                var response = await _httpClient.SendAsync(req, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("AlAdhan tomorrow Fajr BAD: {Status} {Body}", response.StatusCode, body);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var json = JObject.Parse(content);
                return json["data"]?["timings"]?["Fajr"]?.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error fetching tomorrow Fajr by coordinates");
                return null;
            }
        }

        private async Task<string?> GetTomorrowFajrByCityAsync(
            string city,
            string country,
            string? timeZoneId,
            int method,
            int school,
            CancellationToken cancellationToken)
        {
            try
            {
                // Convert timezone to IANA format (AlAdhan requires IANA, not Windows)
                var ianaTz = TzHelper.ToIana(timeZoneId);

                var tomorrow = DateTime.UtcNow.Date.AddDays(1);
                var queryParams = new Dictionary<string, string?>
                {
                    ["date"] = tomorrow.ToString("dd-MM-yyyy"),
                    ["city"] = city,
                    ["country"] = country,
                    ["method"] = method.ToString(),
                    ["school"] = school.ToString(),
                    ["timezonestring"] = ianaTz
                };

                var url = QueryHelpers.AddQueryString($"{AladhanBaseUrl}/timingsByCity", queryParams);

                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                var response = await _httpClient.SendAsync(req, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("AlAdhan tomorrow Fajr (city) BAD: {Status} {Body}", response.StatusCode, body);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var json = JObject.Parse(content);
                return json["data"]?["timings"]?["Fajr"]?.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error fetching tomorrow Fajr by city");
                return null;
            }
        }
    }
}

