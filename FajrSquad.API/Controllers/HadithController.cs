using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Core.DTOs;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using FajrSquad.Core.Entities;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;

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
        public async Task<IActionResult> CreateHadiths([FromBody] JToken payload)
        {
            try
            {
                List<CreateHadithRequest> requests;

                if (payload.Type == JTokenType.Array)
                {
                    // Validazione: deve essere un array di oggetti
                    if (!payload.All(i => i.Type == JTokenType.Object))
                    {
                        return BadRequest(ApiResponse<object>.ErrorResponse("Ogni elemento dell'array deve essere un oggetto JSON valido."));
                    }

                    requests = payload.ToObject<List<CreateHadithRequest>>()!;
                }
                else if (payload.Type == JTokenType.Object)
                {
                    var single = payload.ToObject<CreateHadithRequest>()!;
                    requests = new List<CreateHadithRequest> { single };
                }
                else
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Formato JSON non valido."));
                }

                // Validazione manuale
                var allErrors = new List<string>();
                foreach (var r in requests)
                {
                    var ctx = new ValidationContext(r);
                    var results = new List<ValidationResult>();
                    if (!Validator.TryValidateObject(r, ctx, results, true))
                    {
                        allErrors.AddRange(results.Select(x => x.ErrorMessage!));
                    }
                }

                if (allErrors.Any())
                    return BadRequest(ApiResponse<object>.ValidationErrorResponse(allErrors));

                // Mapping e salvataggio
                var entities = requests.Select(r => new Hadith
                {
                    Text = r.Text,
                    TextArabic = r.TextArabic,
                    Source = r.Source,
                    Category = r.Category,
                    Theme = r.Theme,
                    Priority = r.Priority,
                    Language = r.Language,
                    IsActive = true
                }).ToList();

                await _context.Hadiths.AddRangeAsync(entities);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse(entities, $"{entities.Count} hadith inseriti con successo"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore inserimento hadith: {ex.Message}");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

    }
}