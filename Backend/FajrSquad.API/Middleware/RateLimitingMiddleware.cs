using FajrSquad.Core.DTOs;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

namespace FajrSquad.API.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private static readonly ConcurrentDictionary<string, ClientRequestInfo> _clients = new();

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientId = GetClientIdentifier(context);
            var endpoint = context.Request.Path.ToString();

            // Different limits for different endpoints
            var limit = GetRateLimit(endpoint);
            var window = TimeSpan.FromMinutes(1); // 1 minute window

            if (!IsRequestAllowed(clientId, limit, window))
            {
                _logger.LogWarning("Rate limit exceeded for client {ClientId} on endpoint {Endpoint}", clientId, endpoint);
                await HandleRateLimitExceeded(context);
                return;
            }

            await _next(context);
        }

        private static string GetClientIdentifier(HttpContext context)
        {
            // Try to get user ID from JWT token first
            var userId = context.User?.FindFirst("sub")?.Value ?? 
                        context.User?.FindFirst("nameid")?.Value;
            
            if (!string.IsNullOrEmpty(userId))
                return $"user_{userId}";

            // Fallback to IP address
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private static int GetRateLimit(string endpoint)
        {
            return endpoint.ToLower() switch
            {
                var path when path.Contains("/auth/login") => 5,  // 5 login attempts per minute
                var path when path.Contains("/otp/send") => 3,    // 3 OTP requests per minute
                var path when path.Contains("/fajr/checkin") => 2, // 2 check-in attempts per minute
                _ => 60 // Default: 60 requests per minute
            };
        }

        private static bool IsRequestAllowed(string clientId, int limit, TimeSpan window)
        {
            var now = DateTime.UtcNow;
            
            _clients.AddOrUpdate(clientId, 
                new ClientRequestInfo { RequestTimes = new List<DateTime> { now } },
                (key, existing) =>
                {
                    // Remove old requests outside the window
                    existing.RequestTimes.RemoveAll(time => now - time > window);
                    existing.RequestTimes.Add(now);
                    return existing;
                });

            return _clients[clientId].RequestTimes.Count <= limit;
        }

        private static async Task HandleRateLimitExceeded(HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json";

            var response = new ApiResponse<object>
            {
                Success = false,
                Errors = new List<string> { "Troppi tentativi. Riprova più tardi." },
                Timestamp = DateTime.UtcNow
            };

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        private class ClientRequestInfo
        {
            public List<DateTime> RequestTimes { get; set; } = new();
        }
    }
}