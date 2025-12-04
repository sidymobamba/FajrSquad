using System.Globalization;
using System.Net.Http;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using FajrSquad.Infrastructure.Services;

namespace FajrSquad.Infrastructure.Services.PrayerTimes
{
    /// <summary>
    /// Implementation of reverse geocoding using OpenStreetMap Nominatim
    /// Isolated to PrayerTimes feature only
    /// </summary>
    public class GeolocationService : IGeolocationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GeolocationService> _logger;
        private readonly ICacheService _cacheService;
        private const int CacheHours = 24; // 24 hours cache for reverse geocoding

        public GeolocationService(
            IHttpClientFactory httpClientFactory,
            ILogger<GeolocationService> logger,
            ICacheService cacheService)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _cacheService = cacheService;
        }

        public bool IsValidCoordinates(decimal latitude, decimal longitude)
        {
            return latitude >= -90m && latitude <= 90m &&
                   longitude >= -180m && longitude <= 180m;
        }

        public async Task<GeocodingResult?> ReverseGeocodeAsync(
            decimal latitude,
            decimal longitude,
            CancellationToken cancellationToken = default)
        {
            if (!IsValidCoordinates(latitude, longitude))
            {
                _logger.LogWarning("Invalid coordinates: lat={Latitude}, lng={Longitude}", latitude, longitude);
                return null;
            }

            // Round coordinates to 4 decimal places for cache key (≈11m precision)
            var latRounded = Math.Round(latitude, 4);
            var lngRounded = Math.Round(longitude, 4);
            var cacheKey = $"geo:reverse:{latRounded:F4}:{lngRounded:F4}";

            // Check cache first
            var cached = await _cacheService.GetAsync<GeocodingResult>(cacheKey);
            if (cached != null)
            {
                _logger.LogDebug("Reverse geocoding cache hit for {Latitude}, {Longitude}", latitude, longitude);
                return cached;
            }

            // Usa un HttpClient registrato come "Geolocation"
            var client = _httpClientFactory.CreateClient("Geolocation");

            // ⚠️ URL corretto di Nominatim
            var url = $"https://nominatim.openstreetmap.org/reverse?lat={latitude.ToString(CultureInfo.InvariantCulture)}&lon={longitude.ToString(CultureInfo.InvariantCulture)}&format=json&addressdetails=1";

            // Richiesta con header obbligatori
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("User-Agent", "FajrSquad/1.0 (contact@fajrsquad.app)");
            req.Headers.Add("Accept-Language", "en");

            try
            {
                var res = await client.SendAsync(req, cancellationToken);
                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Nominatim API returned {StatusCode} for lat={Lat}, lng={Lng}",
                        res.StatusCode, latitude, longitude);
                    return null;
                }

                var json = await res.Content.ReadAsStringAsync(cancellationToken);
                var node = JsonNode.Parse(json);
                if (node is null)
                {
                    _logger.LogWarning("Failed to parse Nominatim JSON response for lat={Lat}, lng={Lng}", latitude, longitude);
                    return null;
                }

                var addr = node["address"];
                if (addr is null)
                {
                    _logger.LogWarning("No address found in Nominatim response for lat={Lat}, lng={Lng}", latitude, longitude);
                    return null;
                }

                // 🔹 Parsing robusto del campo city
                var city =
                    (string?)addr["city"] ??
                    (string?)addr["town"] ??
                    (string?)addr["village"] ??
                    (string?)addr["municipality"] ??
                    (string?)addr["county"];

                var country = (string?)addr["country"];

                // 🔹 timezone (opzionale)
                string? tz = null;
                if (node["timezone"] is not null)
                    tz = node["timezone"]!.ToString();
                else
                    tz = TimeZoneInfo.Local.Id; // fallback se non fornito

                // Log what we found for debugging
                _logger.LogDebug("Nominatim response for lat={Latitude}, lng={Longitude}: city={City}, country={Country}, timezone={Timezone}",
                    latitude, longitude, city ?? "(empty)", country ?? "(empty)", tz ?? "(empty)");

                // Return result even if city is empty but country is present (better than nothing)
                if (string.IsNullOrWhiteSpace(country))
                {
                    _logger.LogWarning("Country is empty in Nominatim response for lat={Latitude}, lng={Longitude}. City={City}",
                        latitude, longitude, city ?? "(empty)");
                    // Still return result if we have at least city
                    if (string.IsNullOrWhiteSpace(city))
                    {
                        return null;
                    }
                }

                var result = new GeocodingResult
                {
                    City = city ?? string.Empty,
                    Country = country ?? string.Empty,
                    TimeZoneId = tz
                };

                // Cache the result
                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(CacheHours));
                _logger.LogInformation("Reverse geocoded lat={Latitude}, lng={Longitude} → {City}, {Country}",
                    latitude, longitude, result.City, result.Country);

                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error during reverse geocoding for lat={Latitude}, lng={Longitude}",
                    latitude, longitude);
                return null;
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Reverse geocoding timeout for lat={Latitude}, lng={Longitude}", latitude, longitude);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during reverse geocoding for lat={Latitude}, lng={Longitude}",
                    latitude, longitude);
                return null;
            }
        }
    }
}
