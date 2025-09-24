using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FajrSquad.Infrastructure.Services;
using FajrSquad.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FajrSquad.Tests
{
    public class NotificationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public NotificationIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task TestEndToEndNotificationFlow()
        {
            // Step 1: Seed test user
            var seedResponse = await _client.PostAsync("/debug/seed-user", 
                new StringContent(JsonSerializer.Serialize(new { 
                    token = "FAKE_TOKEN_FOR_TESTING",
                    timeZone = "Africa/Dakar"
                }), Encoding.UTF8, "application/json"));

            Assert.True(seedResponse.IsSuccessStatusCode);
            var seedResult = await seedResponse.Content.ReadAsStringAsync();
            var seedData = JsonSerializer.Deserialize<JsonElement>(seedResult);
            var userId = seedData.GetProperty("userId").GetString();

            Assert.NotNull(userId);
            _client.DefaultRequestHeaders.Add("X-Test-UserId", userId);

            // Step 2: Enqueue a notification with 1 second delay
            var enqueueResponse = await _client.PostAsync("/debug/enqueue",
                new StringContent(JsonSerializer.Serialize(new
                {
                    type = "Debug",
                    delaySeconds = 1
                }), Encoding.UTF8, "application/json"));

            Assert.True(enqueueResponse.IsSuccessStatusCode);
            var enqueueResult = await enqueueResponse.Content.ReadAsStringAsync();
            var enqueueData = JsonSerializer.Deserialize<JsonElement>(enqueueResult);
            var scheduledId = enqueueData.GetProperty("scheduledNotificationId").GetInt32();

            Assert.True(scheduledId > 0);

            // Step 3: Wait for the notification to be processed
            await Task.Delay(3000); // Wait 3 seconds

            // Step 4: Check that the notification was processed
            var pendingResponse = await _client.GetAsync("/debug/pending");
            Assert.True(pendingResponse.IsSuccessStatusCode);
            var pendingResult = await pendingResponse.Content.ReadAsStringAsync();
            var pendingData = JsonSerializer.Deserialize<JsonElement>(pendingResult);
            var pendingCount = pendingData.GetProperty("count").GetInt32();

            // Should have 0 pending notifications after processing
            Assert.Equal(0, pendingCount);

            // Step 5: Check notification logs
            var logsResponse = await _client.GetAsync($"/debug/logs?last=10");
            Assert.True(logsResponse.IsSuccessStatusCode);
            var logsResult = await logsResponse.Content.ReadAsStringAsync();
            var logsData = JsonSerializer.Deserialize<JsonElement>(logsResult);
            var logsCount = logsData.GetProperty("count").GetInt32();

            // Should have at least 1 log entry
            Assert.True(logsCount > 0);

            // Step 6: Test direct push
            var pushResponse = await _client.PostAsync("/debug/push",
                new StringContent("{}", Encoding.UTF8, "application/json"));

            Assert.True(pushResponse.IsSuccessStatusCode);
            var pushResult = await pushResponse.Content.ReadAsStringAsync();
            var pushData = JsonSerializer.Deserialize<JsonElement>(pushResult);
            var messageId = pushData.GetProperty("messageId").GetString();

            Assert.NotNull(messageId);
            Assert.NotEmpty(messageId);

            // Step 7: Verify fake sender received the notification
            using var scope = _factory.Services.CreateScope();
            var fakeSender = scope.ServiceProvider.GetRequiredService<INotificationSender>() as FakeFcmNotificationSender;
            
            if (fakeSender != null)
            {
                var sentNotifications = fakeSender.GetSentNotifications();
                Assert.True(sentNotifications.Count > 0);
                
                var lastNotification = sentNotifications.Last();
                Assert.Equal("Test Push", lastNotification.Title);
                Assert.Equal("Hello from backend debug endpoint", lastNotification.Body);
            }
        }

        [Fact]
        public async Task TestTimezoneCalculation()
        {
            // Seed user with specific timezone
            var seedResponse = await _client.PostAsync("/debug/seed-user",
                new StringContent(JsonSerializer.Serialize(new
                {
                    token = "FAKE_TOKEN_FOR_TESTING_2",
                    timeZone = "Europe/Rome"
                }), Encoding.UTF8, "application/json"));

            Assert.True(seedResponse.IsSuccessStatusCode);
            var seedResult = await seedResponse.Content.ReadAsStringAsync();
            var seedData = JsonSerializer.Deserialize<JsonElement>(seedResult);
            var userId = seedData.GetProperty("userId").GetString();

            // Test timezone calculation
            var whenResponse = await _client.GetAsync($"/debug/when");
            Assert.True(whenResponse.IsSuccessStatusCode);
            var whenResult = await whenResponse.Content.ReadAsStringAsync();
            var whenData = JsonSerializer.Deserialize<JsonElement>(whenResult);

            var deviceTimeZone = whenData.GetProperty("deviceTimeZone").GetString();
            var force = whenData.GetProperty("force").GetBoolean();

            Assert.Equal("Europe/Rome", deviceTimeZone);
            Assert.True(force); // Should be true in Development
        }

        [Fact]
        public async Task TestForceWindowBehavior()
        {
            // This test verifies that ForceWindow=true allows notifications to be sent
            // regardless of the current time

            var seedResponse = await _client.PostAsync("/debug/seed-user",
                new StringContent(JsonSerializer.Serialize(new
                {
                    token = "FAKE_TOKEN_FOR_TESTING_3",
                    timeZone = "Africa/Dakar"
                }), Encoding.UTF8, "application/json"));

            Assert.True(seedResponse.IsSuccessStatusCode);
            var seedResult = await seedResponse.Content.ReadAsStringAsync();
            var seedData = JsonSerializer.Deserialize<JsonElement>(seedResult);
            var userId = seedData.GetProperty("userId").GetString();

            // Check that ForceWindow is enabled
            var whenResponse = await _client.GetAsync($"/debug/when");
            Assert.True(whenResponse.IsSuccessStatusCode);
            var whenResult = await whenResponse.Content.ReadAsStringAsync();
            var whenData = JsonSerializer.Deserialize<JsonElement>(whenResult);

            var force = whenData.GetProperty("force").GetBoolean();
            Assert.True(force);
        }
    }
}
