using FajrSquad.Core.DTOs;

namespace FajrSquad.Infrastructure.Services.PrayerTimes
{
    /// <summary>
    /// Service for fetching prayer times from AlAdhan API
    /// Isolated to PrayerTimes feature only
    /// </summary>
    public interface IPrayerTimesService
    {
        /// <summary>
        /// Gets today's prayer times by coordinates (GPS first)
        /// </summary>
        Task<PrayerTodayResponse?> GetTodayByCoordsAsync(
            double latitude,
            double longitude,
            int method = 3,
            int school = 0,
            string? timeZoneId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets today's prayer times by city/country (fallback)
        /// </summary>
        Task<PrayerTodayResponse?> GetTodayByCityAsync(
            string city,
            string country,
            int method = 3,
            int school = 0,
            string? timeZoneId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets prayer times for a date range by coordinates (GPS first)
        /// </summary>
        Task<PrayerWeekResponse?> GetWeekByCoordsAsync(
            double latitude,
            double longitude,
            int method = 3,
            int school = 0,
            DateTime? startDate = null,
            int days = 7,
            string? timeZoneId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets prayer times for a date range by city/country (fallback)
        /// </summary>
        Task<PrayerWeekResponse?> GetWeekByCityAsync(
            string city,
            string country,
            int method = 3,
            int school = 0,
            DateTime? startDate = null,
            int days = 7,
            string? timeZoneId = null,
            CancellationToken cancellationToken = default);

        // Legacy methods (kept for backward compatibility)
        /// <summary>
        /// Gets today's prayer times by coordinates or city/country
        /// </summary>
        Task<PrayerTodayResponse?> GetTodayPrayerTimesAsync(
            decimal? latitude = null,
            decimal? longitude = null,
            string? city = null,
            string? country = null,
            string? timeZoneId = null,
            int method = 3,
            int school = 0,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets prayer times for a date range (week) by coordinates or city/country
        /// </summary>
        Task<PrayerWeekResponse?> GetWeekPrayerTimesAsync(
            decimal? latitude = null,
            decimal? longitude = null,
            string? city = null,
            string? country = null,
            string? timeZoneId = null,
            int method = 3,
            int school = 0,
            DateTime? startDate = null,
            int days = 7,
            CancellationToken cancellationToken = default);
    }
}

