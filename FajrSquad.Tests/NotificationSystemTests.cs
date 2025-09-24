using Microsoft.Extensions.Logging;
using Moq;
using FajrSquad.Infrastructure.Services;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FajrSquad.Tests
{
    public class NotificationSystemTests
    {
        private readonly Mock<ILogger<MessageBuilder>> _messageBuilderLogger;
        private readonly Mock<ILogger<NotificationPrivacyService>> _privacyLogger;
        private readonly Mock<ILogger<NotificationMetricsService>> _metricsLogger;
        private readonly Mock<IConfiguration> _configuration;
        private readonly FajrDbContext _dbContext;

        public NotificationSystemTests()
        {
            _messageBuilderLogger = new Mock<ILogger<MessageBuilder>>();
            _privacyLogger = new Mock<ILogger<NotificationPrivacyService>>();
            _metricsLogger = new Mock<ILogger<NotificationMetricsService>>();
            _configuration = new Mock<IConfiguration>();

            // Setup in-memory database
            var options = new DbContextOptionsBuilder<FajrDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new FajrDbContext(options);

            // Setup configuration mocks
            _configuration.Setup(c => c["Notifications:BusinessRules:QuietHoursStart"]).Returns("22:00");
            _configuration.Setup(c => c["Notifications:BusinessRules:QuietHoursEnd"]).Returns("06:00");
            _configuration.Setup(c => c["Notifications:BusinessRules:MaxNotificationsPerDay"]).Returns("10");
        }

        [Fact]
        public async Task MessageBuilder_BuildMorningReminder_ReturnsCorrectFormat()
        {
            // Arrange
            var messageBuilder = new MessageBuilder(_dbContext, _messageBuilderLogger.Object);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = "Test User"
            };
            var deviceToken = new DeviceToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = "test-token",
                Language = "it",
                Platform = "Android"
            };

            // Act
            var result = await messageBuilder.BuildMorningReminderAsync(user, deviceToken);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Ricorda Allah", result.Title);
            Assert.Contains(user.Name, result.Body);
            Assert.Equal("morning_reminder", result.CollapseKey);
            Assert.Equal(NotificationPriority.Normal, result.Priority);
        }

        [Fact]
        public async Task MessageBuilder_BuildEveningReminder_ReturnsCorrectFormat()
        {
            // Arrange
            var messageBuilder = new MessageBuilder(_dbContext, _messageBuilderLogger.Object);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = "Test User"
            };
            var deviceToken = new DeviceToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = "test-token",
                Language = "it",
                Platform = "Android"
            };

            // Act
            var result = await messageBuilder.BuildEveningReminderAsync(user, deviceToken);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Prima di dormire", result.Title);
            Assert.Contains(user.Name, result.Body);
            Assert.Equal("evening_reminder", result.CollapseKey);
        }

        [Fact]
        public async Task NotificationPrivacyService_ShouldSendNotification_RespectsUserPreferences()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var privacyService = new NotificationPrivacyService(_dbContext, _privacyLogger.Object, _configuration.Object);

            // Add user preferences
            var preferences = new UserNotificationPreference
            {
                UserId = userId,
                Morning = false, // Disabled
                Evening = true
            };
            _dbContext.UserNotificationPreferences.Add(preferences);
            await _dbContext.SaveChangesAsync();

            // Act & Assert
            var morningResult = await privacyService.ShouldSendNotificationAsync(userId, "MorningReminder", DateTimeOffset.UtcNow);
            var eveningResult = await privacyService.ShouldSendNotificationAsync(userId, "EveningReminder", DateTimeOffset.UtcNow);

            Assert.False(morningResult);
            Assert.True(eveningResult);
        }

        [Fact]
        public async Task NotificationPrivacyService_IsWithinQuietHours_ReturnsCorrectResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var privacyService = new NotificationPrivacyService(_dbContext, _privacyLogger.Object, _configuration.Object);

            // Test during quiet hours (23:00)
            var quietTime = new DateTimeOffset(2024, 1, 15, 23, 0, 0, TimeSpan.Zero);
            var result = await privacyService.IsWithinQuietHoursAsync(userId, quietTime);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task NotificationMetricsService_GetMetrics_ReturnsCorrectData()
        {
            // Arrange
            var metricsService = new NotificationMetricsService(_dbContext, _metricsLogger.Object);
            var userId = Guid.NewGuid();

            // Add test logs
            var logs = new List<NotificationLog>
            {
                new() { UserId = userId, Type = "MorningReminder", Result = "Sent", SentAt = DateTimeOffset.UtcNow },
                new() { UserId = userId, Type = "EveningReminder", Result = "Sent", SentAt = DateTimeOffset.UtcNow },
                new() { UserId = userId, Type = "DailyHadith", Result = "Failed", Error = "Test error", SentAt = DateTimeOffset.UtcNow }
            };

            _dbContext.NotificationLogs.AddRange(logs);
            await _dbContext.SaveChangesAsync();

            // Act
            var metrics = await metricsService.GetMetricsAsync(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow);

            // Assert
            Assert.Equal(2, metrics.TotalSent);
            Assert.Equal(1, metrics.TotalFailed);
            Assert.Equal(66.67, metrics.SuccessRate, 2);
            Assert.Equal(2, metrics.SentByType["MorningReminder"]);
            Assert.Equal(1, metrics.SentByType["EveningReminder"]);
        }

        [Fact]
        public async Task NotificationPrivacyService_HasExceededDailyLimit_ReturnsCorrectResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var privacyService = new NotificationPrivacyService(_dbContext, _privacyLogger.Object, _configuration.Object);

            // Add 11 notification logs (exceeds limit of 10)
            var logs = Enumerable.Range(1, 11).Select(i => new NotificationLog
            {
                UserId = userId,
                Type = "TestNotification",
                Result = "Sent",
                SentAt = DateTimeOffset.UtcNow
            }).ToList();

            _dbContext.NotificationLogs.AddRange(logs);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await privacyService.HasExceededDailyLimitAsync(userId, "TestNotification");

            // Assert
            Assert.True(result);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
