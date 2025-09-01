using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Core.DTOs;
using FajrSquad.Core.Entities;

namespace FajrSquad.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly FajrDbContext _context;
        private readonly ILogger<EventsController> _logger;

        public EventsController(FajrDbContext context, ILogger<EventsController> logger)
        {
            _context = context;
            _logger = logger;
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
