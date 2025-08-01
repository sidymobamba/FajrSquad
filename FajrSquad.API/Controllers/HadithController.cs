using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Core.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FajrSquad.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HadithController : ControllerBase
    {
        private readonly FajrDbContext _context;
        private readonly ILogger<HadithController> _logger;

        public HadithController(FajrDbContext context, ILogger<HadithController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("morning")]
        public async Task<IActionResult> GetMorningHadith([FromQuery] string language = "fr")
        {
            try
            {
                var hadith = await _context.Hadiths
                    .Where(h => h.Category == "morning" && h.Language == language && h.IsActive && !h.IsDeleted)
                    .OrderBy(h => h.Priority)
                    .ThenBy(h => Guid.NewGuid()) // Random among same priority
                    .FirstOrDefaultAsync();

                if (hadith == null)
                {
                    // Fallback to any morning hadith
                    hadith = await _context.Hadiths
                        .Where(h => h.Category == "morning" && h.IsActive && !h.IsDeleted)
                        .OrderBy(h => Guid.NewGuid())
                        .FirstOrDefaultAsync();
                }

                return Ok(ApiResponse<object>.SuccessResponse(hadith));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting morning hadith");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [HttpGet("random")]
        public async Task<IActionResult> GetRandomHadith([FromQuery] string? category = null, [FromQuery] string language = "fr")
        {
            try
            {
                var query = _context.Hadiths
                    .Where(h => h.Language == language && h.IsActive && !h.IsDeleted);

                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(h => h.Category == category);
                }

                var hadith = await query
                    .OrderBy(h => Guid.NewGuid())
                    .FirstOrDefaultAsync();

                return Ok(ApiResponse<object>.SuccessResponse(hadith));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting random hadith");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllHadiths([FromQuery] string? category = null, [FromQuery] string language = "fr", [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Hadiths
                    .Where(h => h.Language == language && h.IsActive && !h.IsDeleted);

                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(h => h.Category == category);
                }

                var totalCount = await query.CountAsync();
                var hadiths = await query
                    .OrderBy(h => h.Priority)
                    .ThenBy(h => h.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var paginatedResponse = new PaginatedResponse<object>
                {
                    Items = hadiths.Cast<object>().ToList(),
                    TotalCount = totalCount,
                    PageNumber = page,
                    PageSize = pageSize
                };

                return Ok(ApiResponse<PaginatedResponse<object>>.SuccessResponse(paginatedResponse));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all hadiths");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.Hadiths
                    .Where(h => h.IsActive && !h.IsDeleted)
                    .Select(h => h.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Ok(ApiResponse<List<string>>.SuccessResponse(categories));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hadith categories");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateHadith([FromBody] CreateHadithRequest request)
        {
            try
            {
                var hadith = new FajrSquad.Core.Entities.Hadith
                {
                    Text = request.Text,
                    TextArabic = request.TextArabic,
                    Source = request.Source,
                    Category = request.Category,
                    Theme = request.Theme,
                    Priority = request.Priority,
                    Language = request.Language,
                    IsActive = true
                };

                _context.Hadiths.Add(hadith);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse(hadith, "Hadith creato con successo"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating hadith");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }
    }
}