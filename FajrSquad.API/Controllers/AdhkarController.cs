using System.Security.Claims;
using FajrSquad.Core.DTOs;
using FajrSquad.Core.DTOs.Adhkar;
using FajrSquad.Core.Entities.Adhkar;
using FajrSquad.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;

namespace FajrSquad.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdhkarController : ControllerBase
    {
        private readonly FajrDbContext _context;
        private readonly ILogger<AdhkarController> _logger;
        private const string DefaultLanguage = "it";

        public AdhkarController(FajrDbContext context, ILogger<AdhkarController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /api/adhkar
        [HttpGet]
        public async Task<IActionResult> GetAllAdhkar([FromQuery] string? category, [FromQuery] string? language)
        {
            try
            {
                var lang = language ?? DefaultLanguage;
                var query = _context.Adhkar
                    .Where(a => a.Language == lang && a.IsActive && !a.IsDeleted);

                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(a => a.Category == category);
                }

                var adhkar = await query
                    .OrderBy(a => a.Priority)
                    .ThenBy(a => a.CreatedAt)
                    .ToListAsync();

                // Se l'utente è autenticato, aggiungi info su preferiti e progressi
                if (TryGetUserId(out var userId))
                {
                    var today = DateTime.UtcNow.Date;
                    var favorites = await _context.UserAdhkarFavorites
                        .Where(f => f.UserId == userId)
                        .Select(f => f.AdhkarId)
                        .ToListAsync();
                    
                    var progress = await _context.UserAdhkarProgress
                        .Where(p => p.UserId == userId && p.Date == today)
                        .ToListAsync();

                    var adhkarResponses = adhkar.Select(a => new AdhkarResponse
                    {
                        Id = a.Id,
                        Arabic = a.Arabic,
                        Transliteration = a.Transliteration,
                        Translation = a.Translation,
                        Repetitions = a.Repetitions,
                        Source = a.Source,
                        Category = a.Category,
                        Priority = a.Priority,
                        Language = a.Language,
                        IsFavorite = favorites.Contains(a.Id),
                        CurrentCount = progress.FirstOrDefault(p => p.AdhkarId == a.Id)?.CurrentCount ?? 0,
                        IsCompleted = progress.FirstOrDefault(p => p.AdhkarId == a.Id)?.IsCompleted ?? false
                    }).ToList();

                    return Ok(ApiResponse<List<AdhkarResponse>>.SuccessResponse(adhkarResponses));
                }

                var responses = adhkar.Select(a => new AdhkarResponse
                {
                    Id = a.Id,
                    Arabic = a.Arabic,
                    Transliteration = a.Transliteration,
                    Translation = a.Translation,
                    Repetitions = a.Repetitions,
                    Source = a.Source,
                    Category = a.Category,
                    Priority = a.Priority,
                    Language = a.Language
                }).ToList();

                return Ok(ApiResponse<List<AdhkarResponse>>.SuccessResponse(responses));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting adhkar");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        // GET /api/adhkar/{category}
        [HttpGet("{category}")]
        public async Task<IActionResult> GetAdhkarByCategory(string category, [FromQuery] string? language)
        {
            try
            {
                var lang = language ?? DefaultLanguage;
                var adhkar = await _context.Adhkar
                    .Where(a => a.Category == category && a.Language == lang && a.IsActive && !a.IsDeleted)
                    .OrderBy(a => a.Priority)
                    .ThenBy(a => a.CreatedAt)
                    .ToListAsync();

                if (TryGetUserId(out var userId))
                {
                    var today = DateTime.UtcNow.Date;
                    var favorites = await _context.UserAdhkarFavorites
                        .Where(f => f.UserId == userId)
                        .Select(f => f.AdhkarId)
                        .ToListAsync();
                    
                    var progress = await _context.UserAdhkarProgress
                        .Where(p => p.UserId == userId && p.Date == today)
                        .ToListAsync();

                    var responses = adhkar.Select(a => new AdhkarResponse
                    {
                        Id = a.Id,
                        Arabic = a.Arabic,
                        Transliteration = a.Transliteration,
                        Translation = a.Translation,
                        Repetitions = a.Repetitions,
                        Source = a.Source,
                        Category = a.Category,
                        Priority = a.Priority,
                        Language = a.Language,
                        IsFavorite = favorites.Contains(a.Id),
                        CurrentCount = progress.FirstOrDefault(p => p.AdhkarId == a.Id)?.CurrentCount ?? 0,
                        IsCompleted = progress.FirstOrDefault(p => p.AdhkarId == a.Id)?.IsCompleted ?? false
                    }).ToList();

                    return Ok(ApiResponse<List<AdhkarResponse>>.SuccessResponse(responses));
                }

                var basicResponses = adhkar.Select(a => new AdhkarResponse
                {
                    Id = a.Id,
                    Arabic = a.Arabic,
                    Transliteration = a.Transliteration,
                    Translation = a.Translation,
                    Repetitions = a.Repetitions,
                    Source = a.Source,
                    Category = a.Category,
                    Priority = a.Priority,
                    Language = a.Language
                }).ToList();

                return Ok(ApiResponse<List<AdhkarResponse>>.SuccessResponse(basicResponses));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting adhkar by category");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        // GET /api/adhkar/stats
        [Authorize]
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                var today = DateTime.UtcNow.Date;
                
                // Completati oggi
                var completedToday = await _context.UserAdhkarProgress
                    .Where(p => p.UserId == userId && p.Date == today && p.IsCompleted)
                    .Select(p => p.AdhkarId)
                    .ToListAsync();

                // Stats aggregate
                var stats = await _context.UserAdhkarStats
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (stats == null)
                {
                    // Crea stats se non esistono
                    stats = new UserAdhkarStats
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        TotalCompleted = 0,
                        CurrentStreak = 0,
                        LongestStreak = 0
                    };
                    await _context.UserAdhkarStats.AddAsync(stats);
                    await _context.SaveChangesAsync();
                }

                var response = new AdhkarStatsResponse
                {
                    CompletedToday = completedToday,
                    TotalCompleted = stats.TotalCompleted,
                    Streak = stats.CurrentStreak,
                    LastCompleted = stats.LastCompletedDate
                };

                return Ok(ApiResponse<AdhkarStatsResponse>.SuccessResponse(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting adhkar stats");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        // POST /api/adhkar/{id}/increment
        [Authorize]
        [HttpPost("{id}/increment")]
        public async Task<IActionResult> IncrementAdhkar(Guid id, [FromBody] IncrementAdhkarRequest? request)
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                var adhkar = await _context.Adhkar
                    .FirstOrDefaultAsync(a => a.Id == id && a.IsActive && !a.IsDeleted);

                if (adhkar == null)
                    return NotFound(ApiResponse<object>.ErrorResponse("Adhkar non trovato"));

                var date = request?.Date?.Date ?? DateTime.UtcNow.Date;
                
                var progress = await _context.UserAdhkarProgress
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.AdhkarId == id && p.Date == date);

                if (progress == null)
                {
                    progress = new UserAdhkarProgress
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        AdhkarId = id,
                        Date = date,
                        CurrentCount = 1,
                        IsCompleted = false
                    };
                    await _context.UserAdhkarProgress.AddAsync(progress);
                }
                else
                {
                    progress.CurrentCount++;
                    progress.UpdatedAt = DateTime.UtcNow;
                }

                // Controlla se completato
                if (progress.CurrentCount >= adhkar.Repetitions && !progress.IsCompleted)
                {
                    progress.IsCompleted = true;
                    progress.CompletedAt = DateTime.UtcNow;
                    
                    // Aggiorna stats
                    await UpdateStats(userId, true);
                }

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse(new
                {
                    currentCount = progress.CurrentCount,
                    isCompleted = progress.IsCompleted,
                    repetitions = adhkar.Repetitions
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing adhkar");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        // POST /api/adhkar/{id}/complete
        [Authorize]
        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteAdhkar(Guid id, [FromBody] CompleteAdhkarRequest? request)
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                var adhkar = await _context.Adhkar
                    .FirstOrDefaultAsync(a => a.Id == id && a.IsActive && !a.IsDeleted);

                if (adhkar == null)
                    return NotFound(ApiResponse<object>.ErrorResponse("Adhkar non trovato"));

                var date = request?.Date?.Date ?? DateTime.UtcNow.Date;
                
                var progress = await _context.UserAdhkarProgress
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.AdhkarId == id && p.Date == date);

                if (progress == null)
                {
                    progress = new UserAdhkarProgress
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        AdhkarId = id,
                        Date = date,
                        CurrentCount = adhkar.Repetitions,
                        IsCompleted = true,
                        CompletedAt = DateTime.UtcNow
                    };
                    await _context.UserAdhkarProgress.AddAsync(progress);
                }
                else
                {
                    progress.CurrentCount = adhkar.Repetitions;
                    progress.IsCompleted = true;
                    progress.CompletedAt = DateTime.UtcNow;
                    progress.UpdatedAt = DateTime.UtcNow;
                }

                await UpdateStats(userId, true);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse(new { completed = true }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing adhkar");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        // POST /api/adhkar/{id}/reset
        [Authorize]
        [HttpPost("{id}/reset")]
        public async Task<IActionResult> ResetAdhkar(Guid id)
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                var today = DateTime.UtcNow.Date;
                var progress = await _context.UserAdhkarProgress
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.AdhkarId == id && p.Date == today);

                if (progress != null)
                {
                    _context.UserAdhkarProgress.Remove(progress);
                    await _context.SaveChangesAsync();
                }

                return Ok(ApiResponse<object>.SuccessResponse(new { reset = true }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting adhkar");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        // GET /api/adhkar/favorites
        [Authorize]
        [HttpGet("favorites")]
        public async Task<IActionResult> GetFavorites([FromQuery] string? language)
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                var lang = language ?? DefaultLanguage;
                var favoriteIds = await _context.UserAdhkarFavorites
                    .Where(f => f.UserId == userId)
                    .Select(f => f.AdhkarId)
                    .ToListAsync();

                var adhkar = await _context.Adhkar
                    .Where(a => favoriteIds.Contains(a.Id) && a.Language == lang && a.IsActive && !a.IsDeleted)
                    .OrderBy(a => a.Priority)
                    .ThenBy(a => a.CreatedAt)
                    .ToListAsync();

                var today = DateTime.UtcNow.Date;
                var progress = await _context.UserAdhkarProgress
                    .Where(p => p.UserId == userId && p.Date == today)
                    .ToListAsync();

                var responses = adhkar.Select(a => new AdhkarResponse
                {
                    Id = a.Id,
                    Arabic = a.Arabic,
                    Transliteration = a.Transliteration,
                    Translation = a.Translation,
                    Repetitions = a.Repetitions,
                    Source = a.Source,
                    Category = a.Category,
                    Priority = a.Priority,
                    Language = a.Language,
                    IsFavorite = true,
                    CurrentCount = progress.FirstOrDefault(p => p.AdhkarId == a.Id)?.CurrentCount ?? 0,
                    IsCompleted = progress.FirstOrDefault(p => p.AdhkarId == a.Id)?.IsCompleted ?? false
                }).ToList();

                return Ok(ApiResponse<List<AdhkarResponse>>.SuccessResponse(responses));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting favorites");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        // POST /api/adhkar/{id}/favorite
        [Authorize]
        [HttpPost("{id}/favorite")]
        public async Task<IActionResult> ToggleFavorite(Guid id, [FromBody] ToggleFavoriteRequest request)
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                var favorite = await _context.UserAdhkarFavorites
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.AdhkarId == id);

                if (request.IsFavorite)
                {
                    if (favorite == null)
                    {
                        favorite = new UserAdhkarFavorite
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            AdhkarId = id
                        };
                        await _context.UserAdhkarFavorites.AddAsync(favorite);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    if (favorite != null)
                    {
                        _context.UserAdhkarFavorites.Remove(favorite);
                        await _context.SaveChangesAsync();
                    }
                }

                return Ok(ApiResponse<object>.SuccessResponse(new { isFavorite = request.IsFavorite }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling favorite");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        // GET /api/adhkar/progress
        [Authorize]
        [HttpGet("progress")]
        public async Task<IActionResult> GetProgress([FromQuery] DateTime? date)
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                var targetDate = date?.Date ?? DateTime.UtcNow.Date;
                var progress = await _context.UserAdhkarProgress
                    .Where(p => p.UserId == userId && p.Date == targetDate)
                    .Include(p => p.Adhkar)
                    .ToListAsync();

                var response = progress.Select(p => new
                {
                    adhkarId = p.AdhkarId,
                    currentCount = p.CurrentCount,
                    isCompleted = p.IsCompleted,
                    completedAt = p.CompletedAt,
                    adhkar = new
                    {
                        id = p.Adhkar?.Id,
                        arabic = p.Adhkar?.Arabic,
                        translation = p.Adhkar?.Translation,
                        repetitions = p.Adhkar?.Repetitions,
                        category = p.Adhkar?.Category
                    }
                }).ToList();

                return Ok(ApiResponse<object>.SuccessResponse(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting progress");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        // POST /api/adhkar (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateAdhkar([FromBody] JToken payload)
        {
            try
            {
                List<CreateAdhkarRequest> requests;

                if (payload.Type == JTokenType.Array)
                {
                    if (!payload.All(i => i.Type == JTokenType.Object))
                        return BadRequest(ApiResponse<object>.ErrorResponse("Ogni elemento dell'array deve essere un oggetto JSON valido."));

                    requests = payload.ToObject<List<CreateAdhkarRequest>>()!;
                }
                else if (payload.Type == JTokenType.Object)
                {
                    var single = payload.ToObject<CreateAdhkarRequest>()!;
                    requests = new List<CreateAdhkarRequest> { single };
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

                // Mapping e salvataggio
                var entities = requests.Select(r => new Adhkar
                {
                    Arabic = r.Arabic,
                    Transliteration = r.Transliteration,
                    Translation = r.Translation,
                    Repetitions = r.Repetitions,
                    Source = r.Source,
                    Category = r.Category,
                    Priority = r.Priority,
                    Language = r.Language ?? DefaultLanguage,
                    IsActive = true
                }).ToList();

                await _context.Adhkar.AddRangeAsync(entities);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse(entities, $"{entities.Count} adhkar inseriti con successo"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore inserimento adhkar: {ex.Message}");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        // Helper methods
        private bool TryGetUserId(out Guid userId)
        {
            userId = Guid.Empty;
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

            if (string.IsNullOrEmpty(userIdClaim))
                return false;

            return Guid.TryParse(userIdClaim, out userId);
        }

        private async Task UpdateStats(Guid userId, bool incrementCompleted = false)
        {
            var stats = await _context.UserAdhkarStats
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (stats == null)
            {
                stats = new UserAdhkarStats
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TotalCompleted = 0,
                    CurrentStreak = 0,
                    LongestStreak = 0
                };
                await _context.UserAdhkarStats.AddAsync(stats);
            }

            if (incrementCompleted)
            {
                stats.TotalCompleted++;
                stats.LastCompletedDate = DateTime.UtcNow.Date;
                
                // Calcola streak
                var yesterday = DateTime.UtcNow.Date.AddDays(-1);
                var hasYesterday = await _context.UserAdhkarProgress
                    .AnyAsync(p => p.UserId == userId && p.Date == yesterday && p.IsCompleted);

                if (hasYesterday || stats.LastCompletedDate == yesterday)
                {
                    stats.CurrentStreak++;
                    if (stats.CurrentStreak > stats.LongestStreak)
                        stats.LongestStreak = stats.CurrentStreak;
                }
                else
                {
                    stats.CurrentStreak = 1;
                }
            }

            stats.UpdatedAt = DateTime.UtcNow;
        }
    }
}

