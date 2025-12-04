namespace FajrSquad.Infrastructure.Services.PrayerTimes
{
    /// <summary>
    /// Result of reverse geocoding operation
    /// </summary>
    public class GeocodingResult
    {
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string? TimeZoneId { get; set; }
    }

    /// <summary>
    /// Service for reverse geocoding (coordinates → city/country/timezone)
    /// Isolated to PrayerTimes feature only
    /// </summary>
    public interface IGeolocationService
    {
        /// <summary>
        /// Performs reverse geocoding to get city, country, and timezone from coordinates
        /// </summary>
        /// <param name="latitude">Latitude (-90 to 90)</param>
        /// <param name="longitude">Longitude (-180 to 180)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Geocoding result or null if reverse geocoding fails</returns>
        Task<GeocodingResult?> ReverseGeocodeAsync(
            decimal latitude,
            decimal longitude,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if coordinates are within valid ranges
        /// </summary>
        bool IsValidCoordinates(decimal latitude, decimal longitude);
    }
}

