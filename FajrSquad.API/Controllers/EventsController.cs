using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Core.DTOs;
using FajrSquad.Core.Entities;
using FajrSquad.Infrastructure.Services;

namespace FajrSquad.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly FajrDbContext _context;
        private readonly ILogger<EventsController> _logger;
        private readonly INotificationSender _notificationSender;
        private readonly IMessageBuilder _messageBuilder;
        private readonly INotificationScheduler _notificationScheduler;

        public EventsController(
            FajrDbContext context, 
            ILogger<EventsController> logger,
            INotificationSender notificationSender,
            IMessageBuilder messageBuilder,
            INotificationScheduler notificationScheduler)
        {
            _context = context;
            _logger = logger;
            _notificationSender = notificationSender;
            _messageBuilder = messageBuilder;
            _notificationScheduler = notificationScheduler;
        }

        // GET api/events
        [HttpGet]
        public async Task<IActionResult> GetAllEvents()
        {
            var events = await _context.Events
                .Where(e => e.IsActive && !e.IsDeleted)
                .OrderBy(e => e.StartDate)
                .ToListAsync();

            return Ok(ApiResponse<object>.SuccessResponse(events));
        }

        // GET api/events/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEvent(Guid id)
        {
            var evt = await _context.Events.FindAsync(id);
            if (evt == null || evt.IsDeleted)
                return NotFound(ApiResponse<object>.ErrorResponse("Evento non trovato"));

            return Ok(ApiResponse<object>.SuccessResponse(evt));
        }

        // POST api/events
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
        {
            var entity = new Event
            {
                Title = request.Title,
                Description = request.Description,
                Location = request.Location,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Organizer = request.Organizer
            };

            await _context.Events.AddAsync(entity);
            await _context.SaveChangesAsync();

            try
            {
                // Send immediate event created notification to all users
                var users = await _context.Users
                    .Where(u => !u.IsDeleted)
                    .Include(u => u.DeviceTokens.Where(dt => dt.IsActive && !dt.IsDeleted))
                    .Include(u => u.UserNotificationPreferences)
                    .ToListAsync();

                foreach (var user in users)
                {
                    try
                    {
                        // Check if user has event notifications enabled
                        var preferences = user.UserNotificationPreferences?.FirstOrDefault();
                        if (preferences != null && !preferences.EventsNew)
                        {
                            continue;
                        }

                        var deviceToken = user.DeviceTokens?.FirstOrDefault();
                        if (deviceToken != null)
                        {
                            var notificationRequest = await _messageBuilder.BuildEventCreatedAsync(entity, user, deviceToken);
                            await _notificationSender.SendToUserAsync(user.Id, notificationRequest);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send event created notification to user {UserId}", user.Id);
                    }
                }

                // Schedule event reminders
                await _notificationScheduler.ScheduleEventRemindersAsync(entity);

                _logger.LogInformation("Event {EventId} created and notifications sent", entity.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notifications for event {EventId}", entity.Id);
                // Don't fail the event creation if notifications fail
            }

            return Ok(ApiResponse<object>.SuccessResponse(entity, "Evento creato con successo"));
        }

        // PUT api/events/{id}
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] CreateEventRequest request)
        {
            var evt = await _context.Events.FindAsync(id);
            if (evt == null || evt.IsDeleted)
                return NotFound(ApiResponse<object>.ErrorResponse("Evento non trovato"));

            evt.Title = request.Title;
            evt.Description = request.Description;
            evt.Location = request.Location;
            evt.StartDate = request.StartDate;
            evt.EndDate = request.EndDate;
            evt.Organizer = request.Organizer;
            evt.UpdatedAt = DateTime.UtcNow;

            _context.Events.Update(evt);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse(evt, "Evento aggiornato con successo"));
        }

        // DELETE api/events/{id}
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(Guid id)
        {
            var evt = await _context.Events.FindAsync(id);
            if (evt == null || evt.IsDeleted)
                return NotFound(ApiResponse<object>.ErrorResponse("Evento non trovato"));

            evt.IsDeleted = true;
            evt.IsActive = false;
            evt.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessResponse(null, "Evento eliminato con successo"));
        }
    }
}
