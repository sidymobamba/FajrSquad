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
        private const string DefaultLanguage = "it";

        public MotivationController(FajrDbContext context, ILogger<MotivationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("evening")]
        public async Task<IActionResult> GetEveningMotivation()
        {
            try
            {
                var motivation = await _context.Motivations
                    .Where(m => m.Type == "night" && m.Language == DefaultLanguage && m.IsActive && !m.IsDeleted)
                    .OrderBy(m => m.Priority)
                    .ThenBy(m => Guid.NewGuid())
                    .FirstOrDefaultAsync();

                if (motivation == null)
                {
                    // fallback senza filtro lingua
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
        public async Task<IActionResult> GetFajrMotivation()
        {
            try
            {
                var motivation = await _context.Motivations
                    .Where(m => m.Type == "fajr" && m.Language == DefaultLanguage && m.IsActive && !m.IsDeleted)
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
        public async Task<IActionResult> GetRandomMotivation()
        {
            try
            {
                var motivation = await _context.Motivations
                    .Where(m => m.Language == DefaultLanguage && m.IsActive && !m.IsDeleted)
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
        public async Task<IActionResult> GetAllMotivations()
        {
            try
            {
                var motivations = await _context.Motivations
                    .Where(m => m.Language == DefaultLanguage && m.IsActive && !m.IsDeleted)
                    .OrderBy(m => m.Priority)
                    .ThenBy(m => m.CreatedAt)
                    .ToListAsync();

                var response = new PaginatedResponse<object>
                {
                    Items = motivations.Cast<object>().ToList(),
                    TotalCount = motivations.Count,
                    PageNumber = 1,
                    PageSize = motivations.Count
                };

                return Ok(ApiResponse<PaginatedResponse<object>>.SuccessResponse(response));
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
                    if (!payload.All(i => i.Type == JTokenType.Object))
                        return BadRequest(ApiResponse<object>.ErrorResponse("Ogni elemento dell'array deve essere un oggetto JSON valido."));

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

                // Validazione
                var allErrors = new List<string>();
                foreach (var r in requests)
                {
                    var ctx = new ValidationContext(r);
                    var results = new List<ValidationResult>();
                    if (!Validator.TryValidateObject(r, ctx, results, true))
                        allErrors.AddRange(results.Select(x => x.ErrorMessage!));
                }
                if (allErrors.Any())
                    return BadRequest(ApiResponse<object>.ValidationErrorResponse(allErrors));

                // Mapping e salvataggio (forziamo lingua = it)
                var entities = requests.Select(r => new Motivation
                {
                    Text = r.Text,
                    Type = r.Type,
                    Theme = r.Theme,
                    Priority = r.Priority,
                    Language = DefaultLanguage,
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
