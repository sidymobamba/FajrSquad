using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Core.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FajrSquad.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RemindersController : ControllerBase
    {
        private readonly FajrDbContext _context;
        private readonly ILogger<RemindersController> _logger;

        public RemindersController(FajrDbContext context, ILogger<RemindersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("sleep")]
        public async Task<IActionResult> GetSleepReminders([FromQuery] string language = "fr")
        {
            try
            {
                var reminders = await _context.Reminders
                    .Where(r => r.Type == "sleep" && r.Language == language && r.IsActive && !r.IsDeleted)
                    .OrderBy(r => r.Priority)
                    .ToListAsync();

                return Ok(ApiResponse<List<object>>.SuccessResponse(reminders.Cast<object>().ToList()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sleep reminders");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [HttpGet("fajr")]
        public async Task<IActionResult> GetFajrReminders([FromQuery] string language = "fr")
        {
            try
            {
                var reminders = await _context.Reminders
                    .Where(r => r.Type == "fajr" && r.Language == language && r.IsActive && !r.IsDeleted)
                    .OrderBy(r => r.Priority)
                    .ToListAsync();

                return Ok(ApiResponse<List<object>>.SuccessResponse(reminders.Cast<object>().ToList()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fajr reminders");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [HttpGet("fasting")]
        public async Task<IActionResult> GetFastingReminders([FromQuery] string language = "fr")
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var reminders = await _context.Reminders
                    .Where(r => r.Type == "fasting" && r.Language == language && r.IsActive && !r.IsDeleted)
                    .Where(r => r.ScheduledDate == null || r.ScheduledDate >= today)
                    .OrderBy(r => r.ScheduledDate)
                    .ThenBy(r => r.Priority)
                    .ToListAsync();

                return Ok(ApiResponse<List<object>>.SuccessResponse(reminders.Cast<object>().ToList()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fasting reminders");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [HttpGet("islamic-holidays")]
        public async Task<IActionResult> GetIslamicHolidays([FromQuery] string language = "fr", [FromQuery] int year = 0)
        {
            try
            {
                if (year == 0) year = DateTime.UtcNow.Year;

                var startDate = new DateTime(year, 1, 1);
                var endDate = new DateTime(year, 12, 31);

                var holidays = await _context.Reminders
                    .Where(r => r.Type == "islamic_holiday" && r.Language == language && r.IsActive && !r.IsDeleted)
                    .Where(r => r.ScheduledDate >= startDate && r.ScheduledDate <= endDate)
                    .OrderBy(r => r.ScheduledDate)
                    .ToListAsync();

                return Ok(ApiResponse<List<object>>.SuccessResponse(holidays.Cast<object>().ToList()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting islamic holidays");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcomingReminders([FromQuery] string language = "fr", [FromQuery] int days = 7)
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var endDate = today.AddDays(days);

                var reminders = await _context.Reminders
                    .Where(r => r.Language == language && r.IsActive && !r.IsDeleted)
                    .Where(r => r.ScheduledDate >= today && r.ScheduledDate <= endDate)
                    .OrderBy(r => r.ScheduledDate)
                    .ThenBy(r => r.Priority)
                    .ToListAsync();

                return Ok(ApiResponse<List<object>>.SuccessResponse(reminders.Cast<object>().ToList()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming reminders");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [HttpGet("by-type/{type}")]
        public async Task<IActionResult> GetRemindersByType(string type, [FromQuery] string language = "fr")
        {
            try
            {
                var reminders = await _context.Reminders
                    .Where(r => r.Type == type && r.Language == language && r.IsActive && !r.IsDeleted)
                    .OrderBy(r => r.Priority)
                    .ThenBy(r => r.ScheduledDate)
                    .ToListAsync();

                return Ok(ApiResponse<List<object>>.SuccessResponse(reminders.Cast<object>().ToList()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reminders by type {Type}", type);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateReminder([FromBody] CreateReminderRequest request)
        {
            try
            {
                var reminder = new FajrSquad.Core.Entities.Reminder
                {
                    Title = request.Title,
                    Message = request.Message,
                    Type = request.Type,
                    Category = request.Category,
                    ScheduledDate = request.ScheduledDate,
                    ScheduledTime = request.ScheduledTime,
                    IsRecurring = request.IsRecurring,
                    RecurrencePattern = request.RecurrencePattern,
                    Priority = request.Priority,
                    Language = request.Language,
                    HijriDate = request.HijriDate,
                    IsHijriCalendar = request.IsHijriCalendar,
                    AdditionalInfo = request.AdditionalInfo,
                    ActionUrl = request.ActionUrl,
                    IsActive = true
                };

                _context.Reminders.Add(reminder);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse(reminder, "Reminder creato con successo"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reminder");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }
    }
}