using FajrSquad.API.Controllers;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;

namespace FajrSquad.API.IntegrationTests
{
    public class NotificationIntegrationTest : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public NotificationIntegrationTest(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace the real FCM sender with a fake one for testing
                    services.AddScoped<INotificationSender, FakeFcmNotificationSender>();
                });
            });
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task TestNotificationFlow_ShouldWork()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<FajrDbContext>();
            var scheduler = scope.ServiceProvider.GetRequiredService<INotificationScheduler>();
            var processor = scope.ServiceProvider.GetRequiredService<ProcessScheduledNotificationsJob>();

            // Clean up any existing test data
            await CleanupTestData(context);

            // Create a test user with device token
            var testUserId = Guid.NewGuid();
            var testUser = new FajrSquad.Core.Entities.User
            {
                Id = testUserId,
                Name = "Test User",
                Email = "test@example.com",
                Phone = "+1234567890",
                City = "Test City",
                Country = "Test Country",
                Role = "User",
                FajrStreak = 0,
                RegisteredAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            var deviceToken = new FajrSquad.Core.Entities.DeviceToken
            {
                Id = 0,
                UserId = testUserId,
                Token = "test_device_token_123",
                Platform = "Android",
                Language = "it",
                TimeZone = "Africa/Dakar",
                AppVersion = "1.0.0",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            var preferences = new FajrSquad.Core.Entities.UserNotificationPreference
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
                QuietHoursStart = TimeSpan.Parse("23:00"),
                QuietHoursEnd = TimeSpan.Parse("06:00"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            context.Users.Add(testUser);
            context.DeviceTokens.Add(deviceToken);
            context.UserNotificationPreferences.Add(preferences);
            await context.SaveChangesAsync();

            // Act 1: Schedule a test notification
            var notificationData = new { UserId = testUserId, TestData = "integration_test" };
            await scheduler.ScheduleNotificationAsync(
                testUserId, 
                "Debug", 
                DateTimeOffset.UtcNow.AddSeconds(1), 
                notificationData, 
                "test_unique_key_123"
            );

            // Verify notification was scheduled
            var scheduledNotification = await context.ScheduledNotifications
                .FirstOrDefaultAsync(sn => sn.UniqueKey == "test_unique_key_123");
            Assert.NotNull(scheduledNotification);
            Assert.Equal("Pending", scheduledNotification.Status);

            // Act 2: Wait a bit and then process the notification
            await Task.Delay(2000); // Wait 2 seconds

            // Create a job context for the processor
            var jobContext = new MockJobExecutionContext();
            await processor.Execute(jobContext);

            // Assert: Check that the notification was processed
            await context.Entry(scheduledNotification).ReloadAsync();
            Assert.Equal("Succeeded", scheduledNotification.Status);
            Assert.NotNull(scheduledNotification.ProcessedAt);

            // Verify notification log was created
            var notificationLog = await context.NotificationLogs
                .FirstOrDefaultAsync(nl => nl.UserId == testUserId && nl.Type == "Debug");
            Assert.NotNull(notificationLog);
            Assert.Equal("Sent", notificationLog.Result);

            // Cleanup
            await CleanupTestData(context);
        }

        private async Task CleanupTestData(FajrDbContext context)
        {
            // Clean up test data
            var testUsers = await context.Users.Where(u => u.Email == "test@example.com").ToListAsync();
            if (testUsers.Any())
            {
                var testUserIds = testUsers.Select(u => u.Id).ToList();
                
                // Remove related data
                context.NotificationLogs.RemoveRange(
                    context.NotificationLogs.Where(nl => testUserIds.Contains(nl.UserId))
                );
                context.ScheduledNotifications.RemoveRange(
                    context.ScheduledNotifications.Where(sn => testUserIds.Contains(sn.UserId!.Value))
                );
                context.DeviceTokens.RemoveRange(
                    context.DeviceTokens.Where(dt => testUserIds.Contains(dt.UserId))
                );
                context.UserNotificationPreferences.RemoveRange(
                    context.UserNotificationPreferences.Where(p => testUserIds.Contains(p.UserId))
                );
                context.Users.RemoveRange(testUsers);
                
                await context.SaveChangesAsync();
            }
        }
    }

    // Mock job context for testing
    public class MockJobExecutionContext : IJobExecutionContext
    {
        public IScheduler Scheduler => throw new NotImplementedException();
        public ITrigger Trigger => throw new NotImplementedException();
        public IJobDetail JobDetail => throw new NotImplementedException();
        public JobDataMap JobDataMap => new JobDataMap();
        public ICalendar Calendar => throw new NotImplementedException();
        public bool Recovering => throw new NotImplementedException();
        public TriggerKey RecoveringTriggerKey => throw new NotImplementedException();
        public int RefireCount => 0;
        public TimeSpan JobRunTime => TimeSpan.Zero;
        public DateTimeOffset? ScheduledFireTimeUtc => DateTimeOffset.UtcNow;
        public DateTimeOffset? PreviousFireTimeUtc => null;
        public DateTimeOffset? NextFireTimeUtc => null;
        public string FireInstanceId => "test_fire_instance";
        public object? Result { get; set; }
        public CancellationToken CancellationToken => CancellationToken.None;
        public void Put(object key, object value) => throw new NotImplementedException();
        public object? Get(object key) => null;
        public T? Get<T>(object key) => default(T);
        public T? Get<T>(string key) => default(T);
    }
}
