using Microsoft.Extensions.Logging;
using TimeZoneConverter;

namespace FajrSquad.Infrastructure.Services
{
    public class TimezoneService : ITimezoneService
    {
        private readonly ILogger<TimezoneService> _logger;
        private const string DefaultTimezone = "Africa/Dakar";

        public TimezoneService(ILogger<TimezoneService> logger)
        {
            _logger = logger;
        }

        public string NormalizeTimezone(string? timezoneId)
        {
            if (string.IsNullOrWhiteSpace(timezoneId))
            {
                _logger.LogDebug("Empty timezone provided, using default: {DefaultTimezone}", DefaultTimezone);
                return DefaultTimezone;
            }

            // Check for common invalid values
            if (timezoneId.Equals("string", StringComparison.OrdinalIgnoreCase) ||
                timezoneId.Equals("null", StringComparison.OrdinalIgnoreCase) ||
                timezoneId.Length < 3)
            {
                _logger.LogWarning("Invalid timezone '{Timezone}' provided, using default: {DefaultTimezone}", 
                    timezoneId, DefaultTimezone);
                return DefaultTimezone;
            }

            try
            {
                // Try to get the timezone info using TimeZoneConverter
                var timeZoneInfo = TZConvert.GetTimeZoneInfo(timezoneId);
                
                // If successful, return the original ID (it's valid)
                _logger.LogDebug("Successfully validated timezone: {Timezone}", timezoneId);
                return timezoneId;
            }
            catch (TimeZoneNotFoundException ex)
            {
                _logger.LogWarning(ex, "Timezone '{Timezone}' not found, using default: {DefaultTimezone}", 
                    timezoneId, DefaultTimezone);
                return DefaultTimezone;
            }
            catch (InvalidTimeZoneException ex)
            {
                _logger.LogWarning(ex, "Invalid timezone '{Timezone}', using default: {DefaultTimezone}", 
                    timezoneId, DefaultTimezone);
                return DefaultTimezone;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error validating timezone '{Timezone}', using default: {DefaultTimezone}", 
                    timezoneId, DefaultTimezone);
                return DefaultTimezone;
            }
        }

        public bool IsValidTimezone(string? timezoneId)
        {
            if (string.IsNullOrWhiteSpace(timezoneId))
                return false;

            if (timezoneId.Equals("string", StringComparison.OrdinalIgnoreCase) ||
                timezoneId.Equals("null", StringComparison.OrdinalIgnoreCase) ||
                timezoneId.Length < 3)
                return false;

            try
            {
                TZConvert.GetTimeZoneInfo(timezoneId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public DateTimeOffset GetCurrentLocalTime(string timezoneId)
        {
            var normalizedTimezone = NormalizeTimezone(timezoneId);
            var timeZoneInfo = TZConvert.GetTimeZoneInfo(normalizedTimezone);
            var utcNow = DateTimeOffset.UtcNow;
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow.DateTime, timeZoneInfo);
        }

        public DateTimeOffset ConvertToLocalTime(DateTimeOffset utcTime, string timezoneId)
        {
            var normalizedTimezone = NormalizeTimezone(timezoneId);
            var timeZoneInfo = TZConvert.GetTimeZoneInfo(normalizedTimezone);
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime.DateTime, timeZoneInfo);
        }

        public DateTimeOffset ConvertToUtcTime(DateTimeOffset localTime, string timezoneId)
        {
            var normalizedTimezone = NormalizeTimezone(timezoneId);
            var timeZoneInfo = TZConvert.GetTimeZoneInfo(normalizedTimezone);
            return TimeZoneInfo.ConvertTimeToUtc(localTime.DateTime, timeZoneInfo);
        }
    }
}
