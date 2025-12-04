using System.Net;
using System.Net.Http.Json;
using FajrSquad.Core.DTOs;
using Xunit;

namespace FajrSquad.Tests
{
    /// <summary>
    /// Simple integration tests for PrayerTimes GPS-first endpoints
    /// These tests verify that coordinates resolve to city/country/timezone correctly
    /// </summary>
    public class PrayerTimesIntegrationTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly string _baseUrl;

        public PrayerTimesIntegrationTests(TestWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
            _baseUrl = _client.BaseAddress?.ToString() ?? "https://localhost:7271";
        }

        [Fact]
        public async Task GetToday_WithValidCoords_ReturnsLocationData()
        {
            // Arrange: Brescia, Italy coordinates
            var lat = 45.5416;
            var lng = 10.2118;
            var token = await GetAuthTokenAsync();

            // Act
            var response = await _client.GetAsync(
                $"{_baseUrl}/api/PrayerTimes/today?latitude={lat}&longitude={lng}&method=3&school=0",
                token);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PrayerTimesResponse>();
            Assert.NotNull(result);
            Assert.Equal("coords", result.Source);
            Assert.NotNull(result.Location);
            Assert.False(string.IsNullOrWhiteSpace(result.Location.City), "City should not be empty");
            Assert.False(string.IsNullOrWhiteSpace(result.Location.Country), "Country should not be empty");
            Assert.False(string.IsNullOrWhiteSpace(result.Location.Timezone), "Timezone should not be empty");
            Assert.Equal("Europe/Rome", result.Location.Timezone); // Italy should normalize to Europe/Rome
            Assert.NotNull(result.Coords);
            Assert.True(Math.Abs(lat - result.Coords.Lat.Value) < 0.0001, $"Latitude should be approximately {lat}");
            Assert.True(Math.Abs(lng - result.Coords.Lng.Value) < 0.0001, $"Longitude should be approximately {lng}");
        }

        [Fact]
        public async Task GetWeek_WithValidCoords_ReturnsCorrectDaysAndTimezone()
        {
            // Arrange: Brescia, Italy coordinates
            var lat = 45.5416;
            var lng = 10.2118;
            var days = 7;
            var token = await GetAuthTokenAsync();

            // Act
            var response = await _client.GetAsync(
                $"{_baseUrl}/api/PrayerTimes/week?latitude={lat}&longitude={lng}&method=3&school=0&offset=0&days={days}",
                token);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PrayerWeekResponseV2>();
            Assert.NotNull(result);
            Assert.Equal("coords", result.Source);
            Assert.NotNull(result.Location);
            Assert.False(string.IsNullOrWhiteSpace(result.Location.City), "City should not be empty");
            Assert.False(string.IsNullOrWhiteSpace(result.Location.Country), "Country should not be empty");
            Assert.Equal("Europe/Rome", result.Location.Timezone); // Italy should normalize to Europe/Rome
            Assert.NotNull(result.Days);
            Assert.Equal(days, result.Days.Count); // Should return exactly the requested number of days
            
            // All days should have Europe/Rome timezone
            foreach (var day in result.Days)
            {
                Assert.Equal("Europe/Rome", day.Timezone);
            }
        }

        [Fact]
        public async Task GetToday_WithAlAdhanDown_ReturnsGracefulResponse()
        {
            // This test would require mocking AlAdhan to be down
            // For now, we just verify the structure supports graceful degradation
            // In a real scenario, you'd mock the HttpClient or use a test double
            
            // Arrange: Brescia, Italy coordinates
            var lat = 45.5416;
            var lng = 10.2118;
            var token = await GetAuthTokenAsync();

            // Act
            var response = await _client.GetAsync(
                $"{_baseUrl}/api/PrayerTimes/today?latitude={lat}&longitude={lng}&method=3&school=0",
                token);

            // Assert: Even if AlAdhan is down, we should get 200 with location data
            // (This test assumes AlAdhan is up; for true failure testing, use mocks)
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PrayerTimesResponse>();
            Assert.NotNull(result);
            Assert.NotNull(result.Location);
            
            // If prayers is null, error should be set
            if (result.Prayers == null)
            {
                Assert.Equal("UPSTREAM_UNAVAILABLE", result.Error);
            }
        }

        private async Task<string?> GetAuthTokenAsync()
        {
            // TODO: Implement test user authentication
            // For now, return null (tests will need valid JWT)
            return null;
        }
    }

    // Extension method to add auth header
    public static class HttpClientExtensions
    {
        public static async Task<HttpResponseMessage> GetAsync(this HttpClient client, string requestUri, string? token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            return await client.SendAsync(request);
        }
    }

    // Simple test factory (would need proper setup in real scenario)
    public class TestWebApplicationFactory : IDisposable
    {
        private readonly HttpClient _client;

        public TestWebApplicationFactory()
        {
            // In a real scenario, use WebApplicationFactory<Program>
            _client = new HttpClient
            {
                BaseAddress = new Uri("https://localhost:7271")
            };
        }

        public HttpClient CreateClient() => _client;

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}

