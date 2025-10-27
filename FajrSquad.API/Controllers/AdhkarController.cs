using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Core.DTOs;
using FajrSquad.Core.DTOs.Adhkar;
using FajrSquad.Core.Entities.Adhkar;
using FajrSquad.Infrastructure.Services.Adhkar;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AutoMapper;

namespace FajrSquad.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AdhkarController : ControllerBase
    {
        private readonly FajrDbContext _context;
        private readonly IAdhkarService _adhkarService;
        private readonly IMapper _mapper;
        private readonly ILogger<AdhkarController> _logger;
        private const string DefaultLanguage = "it";

        public AdhkarController(
            FajrDbContext context, 
            IAdhkarService adhkarService,
            IMapper mapper,
            ILogger<AdhkarController> logger)
        {
            _context = context;
            _adhkarService = adhkarService;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet("sets")]
        public async Task<IActionResult> GetSets([FromQuery] string type = "morning", [FromQuery] string lang = "it")
        {
            try
            {
                var sets = await _context.AdhkarSets
                    .Where(s => s.Type == type)
                    .Include(s => s.Items)
                        .ThenInclude(i => i.Adhkar)
                            .ThenInclude(a => a.Texts)
                    .OrderBy(s => s.Ord)
                    .ToListAsync();

                var setDtos = _mapper.Map<List<AdhkarSetDto>>(sets);
                return Ok(ApiResponse<List<AdhkarSetDto>>.SuccessResponse(setDtos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting adhkar sets");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAdhkar(
            [FromQuery] string? category = null,
            [FromQuery] string lang = "it",
            [FromQuery] string? q = null,
            [FromQuery] int limit = 20,
            [FromQuery] int offset = 0)
        {
            try
            {
                var adhkar = await _context.Adhkar
                    .Where(a => a.Visible)
                    .Include(a => a.Texts)
                    .ToListAsync();

                // Apply filters in memory
                if (!string.IsNullOrEmpty(category))
                {
                    adhkar = adhkar.Where(a => a.Categories.Contains(category)).ToList();
                }

                if (!string.IsNullOrEmpty(q))
                {
                    adhkar = adhkar.Where(a => 
                        a.Texts.Any(t => 
                            (t.Translation != null && t.Translation.Contains(q)) ||
                            (t.Transliteration != null && t.Transliteration.Contains(q)) ||
                            (t.TextAr != null && t.TextAr.Contains(q))
                        )).ToList();
                }

                var totalCount = adhkar.Count;
                adhkar = adhkar
                    .OrderBy(a => a.Priority)
                    .ThenBy(a => a.Code)
                    .Skip(offset)
                    .Take(limit)
                    .ToList();

                // Filter texts by language after loading
                foreach (var item in adhkar)
                {
                    item.Texts = item.Texts.Where(t => t.Lang == lang || t.Lang == "ar").ToList();
                }

                var adhkarDtos = _mapper.Map<List<AdhkarDto>>(adhkar);
                
                var response = new PaginatedResponse<AdhkarDto>
                {
                    Items = adhkarDtos,
                    TotalCount = totalCount,
                    PageNumber = (offset / limit) + 1,
                    PageSize = limit
                };

                return Ok(ApiResponse<PaginatedResponse<AdhkarDto>>.SuccessResponse(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting adhkar");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [HttpGet("{code}")]
        public async Task<IActionResult> GetAdhkarByCode([FromRoute] string code, [FromQuery] string lang = "it")
        {
            try
            {
                var adhkar = await _context.Adhkar
                    .Where(a => a.Code == code && a.Visible)
                    .Include(a => a.Texts.Where(t => t.Lang == lang || t.Lang == "ar"))
                    .FirstOrDefaultAsync();

                if (adhkar == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Adhkar non trovato"));
                }

                var adhkarDto = _mapper.Map<AdhkarDto>(adhkar);
                return Ok(ApiResponse<AdhkarDto>.SuccessResponse(adhkarDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting adhkar by code");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [Authorize]
        [HttpGet("progress/today")]
        public async Task<IActionResult> GetTodayProgress()
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));
                }

                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var progress = await _adhkarService.GetProgressAsync(userId.Value, today);

                if (progress == null)
                {
                    progress = await _adhkarService.UpsertProgressAsync(userId.Value, today);
                }

                var progressDto = _mapper.Map<UserAdhkarProgressDto>(progress);
                return Ok(ApiResponse<UserAdhkarProgressDto>.SuccessResponse(progressDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today's progress");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [Authorize]
        [HttpPost("progress/count")]
        public async Task<IActionResult> UpdateCount([FromBody] UpdateAdhkarCountRequest request)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<object>.ValidationErrorResponse(
                        ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
                }

                await _adhkarService.UpdateCountAsync(userId.Value, request.DateUtc, request.AdhkarCode, request.Delta);
                
                return Ok(ApiResponse<object>.SuccessResponse(null, "Conteggio aggiornato con successo"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating adhkar count");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [Authorize]
        [HttpPost("progress/complete")]
        public async Task<IActionResult> CompleteWindow([FromBody] CompleteAdhkarWindowRequest request)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<object>.ValidationErrorResponse(
                        ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
                }

                await _adhkarService.CompleteWindowAsync(userId.Value, request.DateUtc, request.Window, request.SetCode);
                
                return Ok(ApiResponse<object>.SuccessResponse(null, "Finestra completata con successo"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing adhkar window");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [Authorize]
        [HttpPost("bookmarks")]
        public async Task<IActionResult> CreateBookmark([FromBody] CreateBookmarkRequest request)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<object>.ValidationErrorResponse(
                        ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
                }

                var adhkar = await _context.Adhkar.FirstOrDefaultAsync(a => a.Code == request.AdhkarCode);
                if (adhkar == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Adhkar non trovato"));
                }

                var existingBookmark = await _context.UserAdhkarBookmarks
                    .FirstOrDefaultAsync(b => b.UserId == userId.Value && b.AdhkarId == adhkar.Id);

                if (existingBookmark != null)
                {
                    return Conflict(ApiResponse<object>.ErrorResponse("Bookmark già esistente"));
                }

                var bookmark = new UserAdhkarBookmark
                {
                    UserId = userId.Value,
                    AdhkarId = adhkar.Id,
                    Note = request.Note
                };

                _context.UserAdhkarBookmarks.Add(bookmark);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse(null, "Bookmark creato con successo"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bookmark");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [Authorize]
        [HttpDelete("bookmarks/{code}")]
        public async Task<IActionResult> DeleteBookmark([FromRoute] string code)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));
                }

                var adhkar = await _context.Adhkar.FirstOrDefaultAsync(a => a.Code == code);
                if (adhkar == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Adhkar non trovato"));
                }

                var bookmark = await _context.UserAdhkarBookmarks
                    .FirstOrDefaultAsync(b => b.UserId == userId.Value && b.AdhkarId == adhkar.Id);

                if (bookmark == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Bookmark non trovato"));
                }

                _context.UserAdhkarBookmarks.Remove(bookmark);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse(null, "Bookmark eliminato con successo"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting bookmark");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        private Guid? GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }
}
