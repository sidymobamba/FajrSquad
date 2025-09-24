using FajrSquad.Core.Entities;
using FajrSquad.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FajrSquad.Infrastructure.Data.Seeders
{
    public class TestUserSeeder
    {
        private readonly FajrDbContext _context;
        private readonly ILogger<TestUserSeeder> _logger;

        public TestUserSeeder(FajrDbContext context, ILogger<TestUserSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Guid> SeedTestUserAsync(string? deviceToken = null, string? timeZone = null)
        {
            var testEmail = "test@fajrsquad.local";
            var testPhone = "+2210000000";
            var testUserId = Guid.NewGuid();

            // Check if test user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == testEmail || u.Phone == testPhone);

            if (existingUser != null)
            {
                _logger.LogInformation("Test user already exists with ID: {UserId}", existingUser.Id);
                testUserId = existingUser.Id;
            }
            else
            {
                // Create test user
                var testUser = new User
                {
                    Id = testUserId,
                    Name = "Test User",
                    Email = testEmail,
                    Phone = testPhone,
                    City = "Dakar",
                    Country = "Senegal",
                    Role = "User",
                    FajrStreak = 0,
                    RegisteredAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                _context.Users.Add(testUser);
                _logger.LogInformation("Created test user with ID: {UserId}", testUserId);
            }

            // Create or update notification preferences
            var preferences = await _context.UserNotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == testUserId);

            if (preferences == null)
            {
                preferences = new UserNotificationPreference
                {
                    UserId = testUserId,
                    Morning = true,
                    Evening = true,
                    FajrMissed = true,
                    Escalation = true,
                    HadithDaily = true,
                    MotivationDaily = true,
                    EventsNew = true,
                    EventsReminder = true,
                    QuietHoursStart = new TimeSpan(22, 0, 0), // 10 PM
                    QuietHoursEnd = new TimeSpan(6, 0, 0),    // 6 AM
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                _context.UserNotificationPreferences.Add(preferences);
                _logger.LogInformation("Created notification preferences for user: {UserId}", testUserId);
            }
            else
            {
                // Update existing preferences to ensure all are enabled
                preferences.Morning = true;
                preferences.Evening = true;
                preferences.FajrMissed = true;
                preferences.Escalation = true;
                preferences.HadithDaily = true;
                preferences.MotivationDaily = true;
                preferences.EventsNew = true;
                preferences.EventsReminder = true;
                preferences.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("Updated notification preferences for user: {UserId}", testUserId);
            }

            // Create or update device token
            var deviceTokenValue = deviceToken ?? "FAKE_TOKEN_FOR_TESTING";
            var timeZoneValue = timeZone ?? "Africa/Dakar";

            var existingDeviceToken = await _context.DeviceTokens
                .FirstOrDefaultAsync(dt => dt.UserId == testUserId && dt.Token == deviceTokenValue);

            if (existingDeviceToken == null)
            {
                var newDeviceToken = new DeviceToken
                {
                    Id = 0, // Will be auto-generated
                    UserId = testUserId,
                    Token = deviceTokenValue,
                    Platform = "Android",
                    Language = "it",
                    TimeZone = timeZoneValue,
                    AppVersion = "1.0.0",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                _context.DeviceTokens.Add(newDeviceToken);
                _logger.LogInformation("Created device token for user: {UserId} with timezone: {TimeZone}", testUserId, timeZoneValue);
            }
            else
            {
                // Update existing device token
                existingDeviceToken.TimeZone = timeZoneValue;
                existingDeviceToken.IsActive = true;
                existingDeviceToken.UpdatedAt = DateTimeOffset.UtcNow;

                _logger.LogInformation("Updated device token for user: {UserId} with timezone: {TimeZone}", testUserId, timeZoneValue);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully seeded test user: {UserId} with token: {Token} and timezone: {TimeZone}", 
                testUserId, deviceTokenValue, timeZoneValue);

            return testUserId;
        }

        public async Task SeedMultipleTestUsersAsync()
        {
            var testUsers = new[]
            {
                new { Email = "user1@fajrsquad.local", Phone = "+2210000001", TimeZone = "Africa/Dakar" },
                new { Email = "user2@fajrsquad.local", Phone = "+2210000002", TimeZone = "Europe/Rome" },
                new { Email = "user3@fajrsquad.local", Phone = "+2210000003", TimeZone = "Europe/Paris" }
            };

            foreach (var user in testUsers)
            {
                await SeedTestUserAsync($"FAKE_TOKEN_{user.Phone}", user.TimeZone);
            }

            _logger.LogInformation("Seeded {Count} additional test users", testUsers.Length);
        }
    }
}
