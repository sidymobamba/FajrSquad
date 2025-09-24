using System;

namespace FajrSquad.Infrastructure.Services
{
    public interface ITimezoneService
    {
        /// <summary>
        /// Normalizes and validates a timezone identifier, returning a valid IANA timezone ID
        /// </summary>
        /// <param name="timezoneId">The timezone identifier to normalize</param>
        /// <returns>A valid IANA timezone ID, or "Africa/Dakar" as fallback</returns>
        string NormalizeTimezone(string? timezoneId);

        /// <summary>
        /// Validates if a timezone identifier is valid
        /// </summary>
        /// <param name="timezoneId">The timezone identifier to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        bool IsValidTimezone(string? timezoneId);

        /// <summary>
        /// Gets the current local time for a given timezone
        /// </summary>
        /// <param name="timezoneId">The IANA timezone identifier</param>
        /// <returns>The current local time in the specified timezone</returns>
        DateTimeOffset GetCurrentLocalTime(string timezoneId);

        /// <summary>
        /// Converts UTC time to local time in the specified timezone
        /// </summary>
        /// <param name="utcTime">The UTC time to convert</param>
        /// <param name="timezoneId">The IANA timezone identifier</param>
        /// <returns>The local time in the specified timezone</returns>
        DateTimeOffset ConvertToLocalTime(DateTimeOffset utcTime, string timezoneId);

        /// <summary>
        /// Converts local time to UTC time
        /// </summary>
        /// <param name="localTime">The local time to convert</param>
        /// <param name="timezoneId">The IANA timezone identifier</param>
        /// <returns>The UTC time</returns>
        DateTimeOffset ConvertToUtcTime(DateTimeOffset localTime, string timezoneId);
    }
}
