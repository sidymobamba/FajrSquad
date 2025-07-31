using FajrSquad.Core.DTOs;
using System.Net;
using System.Text.Json;

namespace FajrSquad.API.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            var response = exception switch
            {
                ArgumentException => new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { exception.Message },
                    Timestamp = DateTime.UtcNow
                },
                UnauthorizedAccessException => new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { "Accesso non autorizzato" },
                    Timestamp = DateTime.UtcNow
                },
                _ => new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { "Si è verificato un errore interno del server" },
                    Timestamp = DateTime.UtcNow
                }
            };

            context.Response.StatusCode = exception switch
            {
                ArgumentException => (int)HttpStatusCode.BadRequest,
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                _ => (int)HttpStatusCode.InternalServerError
            };

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }
}