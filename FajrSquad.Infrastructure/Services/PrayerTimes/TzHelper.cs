using TimeZoneConverter;

namespace FajrSquad.Infrastructure.Services.PrayerTimes
{
    /// <summary>
    /// Helper for timezone conversion (Windows → IANA)
    /// AlAdhan API requires IANA timezone IDs (e.g., "Europe/Rome")
    /// </summary>
    public static class TzHelper
    {
        /// <summary>
        /// Converts Windows timezone ID to IANA timezone ID
        /// Uses TimeZoneConverter library if available, falls back to common mappings
        /// </summary>
        public static string ToIana(string? tzId)
        {
            if (string.IsNullOrWhiteSpace(tzId))
                return "Europe/Rome"; // Default fallback

            // If already IANA format (contains '/'), return as-is
            if (tzId.Contains('/'))
                return tzId;

            // Try TimeZoneConverter first (preferred)
            try
            {
                return TZConvert.WindowsToIana(tzId);
            }
            catch
            {
                // Fall through to manual mapping
            }

            // Common Windows → IANA mappings (fallback)
            var windowsToIana = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "W. Europe Standard Time", "Europe/Rome" },
                { "Central European Standard Time", "Europe/Rome" },
                { "GMT Standard Time", "Europe/London" },
                { "Eastern Standard Time", "America/New_York" },
                { "Pacific Standard Time", "America/Los_Angeles" },
                { "Central Standard Time", "America/Chicago" },
                { "Mountain Standard Time", "America/Denver" },
                { "Arab Standard Time", "Asia/Riyadh" },
                { "Turkey Standard Time", "Europe/Istanbul" },
                { "Egypt Standard Time", "Africa/Cairo" },
                { "Morocco Standard Time", "Africa/Casablanca" },
                { "Algeria Standard Time", "Africa/Algiers" },
                { "Tunisia Standard Time", "Africa/Tunis" },
                { "Libya Standard Time", "Africa/Tripoli" },
                { "Sudan Standard Time", "Africa/Khartoum" },
                { "E. Europe Standard Time", "Europe/Bucharest" },
                { "Russian Standard Time", "Europe/Moscow" },
                { "India Standard Time", "Asia/Kolkata" },
                { "China Standard Time", "Asia/Shanghai" },
                { "Tokyo Standard Time", "Asia/Tokyo" },
                { "Korea Standard Time", "Asia/Seoul" },
                { "AUS Eastern Standard Time", "Australia/Sydney" },
                { "AUS Central Standard Time", "Australia/Darwin" },
                { "New Zealand Standard Time", "Pacific/Auckland" }
            };

            // If already IANA format (contains '/'), return as-is
            if (tzId.Contains('/'))
                return tzId;

            // Try Windows → IANA mapping
            if (windowsToIana.TryGetValue(tzId, out var iana))
                return iana;

            // If not found in mapping, try to use as-is (might already be IANA)
            // But log a warning for unknown Windows timezones
            if (tzId.Contains("Standard Time") || tzId.Contains("Daylight Time"))
            {
                // Likely a Windows timezone we don't have mapped
                // Default to Europe/Rome as safe fallback
                return "Europe/Rome";
            }

            // Assume it's already IANA or return as-is
            return tzId;
        }

        /// <summary>
        /// Gets a valid IANA timezone ID, with fallback to Europe/Rome
        /// </summary>
        public static string GetIanaTimezone(string? tzId)
        {
            if (string.IsNullOrWhiteSpace(tzId))
                return "Europe/Rome";

            return ToIana(tzId);
        }

        /// <summary>
        /// Normalizes timezone based on country (e.g., Italy → Europe/Rome)
        /// Some APIs return Europe/Berlin for Central Europe, but we want country-specific timezones
        /// </summary>
        public static string NormalizeTimezoneByCountry(string? timezone, string? country)
        {
            // Country-based normalization (more accurate than API defaults)
            if (!string.IsNullOrWhiteSpace(country))
            {
                var countryLower = country.Trim();
                
                // Italy should always use Europe/Rome (not Europe/Berlin)
                if (countryLower.Equals("Italy", StringComparison.OrdinalIgnoreCase) ||
                    countryLower.Equals("Italia", StringComparison.OrdinalIgnoreCase))
                {
                    return "Europe/Rome";
                }
                
                // Add other country-specific mappings as needed
                // France → Europe/Paris
                if (countryLower.Equals("France", StringComparison.OrdinalIgnoreCase) ||
                    countryLower.Equals("Francia", StringComparison.OrdinalIgnoreCase))
                {
                    return "Europe/Paris";
                }
                
                // Spain → Europe/Madrid
                if (countryLower.Equals("Spain", StringComparison.OrdinalIgnoreCase) ||
                    countryLower.Equals("España", StringComparison.OrdinalIgnoreCase) ||
                    countryLower.Equals("Spagna", StringComparison.OrdinalIgnoreCase))
                {
                    return "Europe/Madrid";
                }
            }

            // If no country match, use the provided timezone (converted to IANA)
            return ToIana(timezone);
        }
    }
}

