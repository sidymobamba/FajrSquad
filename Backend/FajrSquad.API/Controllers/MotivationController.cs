using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Core.DTOs;
using Microsoft.EntityFrameworkCore;
using FajrSquad.Core.Entities;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;

namespace FajrSquad.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MotivationController : ControllerBase
    {
        private readonly FajrDbContext _context;
        private readonly ILogger<MotivationController> _logger;

        public MotivationController(FajrDbContext context, ILogger<MotivationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("evening")]
        public async Task<IActionResult> GetEveningMotivation([FromQuery] string language = "fr")
        {
            try
            {
                var motivation = await _context.Motivations
                    .Where(m => m.Type == "night" && m.Language == language && m.IsActive && !m.IsDeleted)
                    .OrderBy(m => m.Priority)
                    .ThenBy(m => Guid.NewGuid())
                    .FirstOrDefaultAsync();

                if (motivation == null)
                {
                    // Fallback to any evening motivation
                    motivation = await _context.Motivations
                        .Where(m => m.Type == "night" && m.IsActive && !m.IsDeleted)
                        .OrderBy(m => Guid.NewGuid())
                        .FirstOrDefaultAsync();
                }

                return Ok(ApiResponse<object>.SuccessResponse(motivation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting evening motivation");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [HttpGet("fajr")]
        public async Task<IActionResult> GetFajrMotivation([FromQuery] string language = "fr")
        {
            try
            {
                var motivation = await _context.Motivations
                    .Where(m => m.Type == "fajr" && m.Language == language && m.IsActive && !m.IsDeleted)
                    .OrderBy(m => m.Priority)
                    .ThenBy(m => Guid.NewGuid())
                    .FirstOrDefaultAsync();

                return Ok(ApiResponse<object>.SuccessResponse(motivation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fajr motivation");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [HttpGet("random")]
        public async Task<IActionResult> GetRandomMotivation([FromQuery] string? type = null, [FromQuery] string language = "fr")
        {
            try
            {
                var query = _context.Motivations
                    .Where(m => m.Language == language && m.IsActive && !m.IsDeleted);

                if (!string.IsNullOrEmpty(type))
                {
                    query = query.Where(m => m.Type == type);
                }

                var motivation = await query
                    .OrderBy(m => Guid.NewGuid())
                    .FirstOrDefaultAsync();

                return Ok(ApiResponse<object>.SuccessResponse(motivation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting random motivation");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllMotivations([FromQuery] string? type = null, [FromQuery] string language = "fr", [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Motivations
                    .Where(m => m.Language == language && m.IsActive && !m.IsDeleted);

                if (!string.IsNullOrEmpty(type))
                {
                    query = query.Where(m => m.Type == type);
                }

                var totalCount = await query.CountAsync();
                var motivations = await query
                    .OrderBy(m => m.Priority)
                    .ThenBy(m => m.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var paginatedResponse = new PaginatedResponse<object>
                {
                    Items = motivations.Cast<object>().ToList(),
                    TotalCount = totalCount,
                    PageNumber = page,
                    PageSize = pageSize
                };

                return Ok(ApiResponse<PaginatedResponse<object>>.SuccessResponse(paginatedResponse));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all motivations");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateMotivations([FromBody] JToken payload)
        {
            try
            {
                List<CreateMotivationRequest> requests;

                if (payload.Type == JTokenType.Array)
                {
                    // Validazione: deve essere un array di oggetti
                    if (!payload.All(i => i.Type == JTokenType.Object))
                    {
                        return BadRequest(ApiResponse<object>.ErrorResponse("Ogni elemento dell'array deve essere un oggetto JSON valido."));
                    }

                    requests = payload.ToObject<List<CreateMotivationRequest>>()!;
                }
                else if (payload.Type == JTokenType.Object)
                {
                    var single = payload.ToObject<CreateMotivationRequest>()!;
                    requests = new List<CreateMotivationRequest> { single };
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
                var entities = requests.Select(r => new Motivation
                {
                    Text = r.Text,
                    Type = r.Type,
                    Theme = r.Theme,
                    Priority = r.Priority,
                    Language = r.Language,
                    Author = r.Author,
                    IsActive = true
                }).ToList();

                await _context.Motivations.AddRangeAsync(entities);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse(entities, $"{entities.Count} motivazioni inserite con successo"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore inserimento motivation: {ex.Message}");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

    }
}