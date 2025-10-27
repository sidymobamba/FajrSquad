using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Core.DTOs;
using FajrSquad.Core.DTOs.Adhkar;
using FajrSquad.Core.Entities.Adhkar;

namespace FajrSquad.API.Controllers
{
    [ApiController]
    [Route("api/v1/admin/adhkar")]
    [Authorize(Roles = "Admin")]
    public class AdminAdhkarController : ControllerBase
    {
        private readonly FajrDbContext _db;
        private readonly ILogger<AdminAdhkarController> _logger;

        public AdminAdhkarController(FajrDbContext db, ILogger<AdminAdhkarController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // 1) CREA/AGGIORNA ADHKAR (metadati)
        [HttpPost]
        public async Task<IActionResult> UpsertAdhkar([FromBody] CreateAdhkarRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.ValidationErrorResponse(
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

            var entity = await _db.Adhkar.FirstOrDefaultAsync(a => a.Code == req.Code);
            if (entity is null)
            {
                entity = new Adhkar
                {
                    Id = Guid.NewGuid(),
                    Code = req.Code,
                    Categories = req.Categories,
                    Priority = req.Priority,
                    Repetitions = req.Repetitions,
                    SourceBook = req.SourceBook,
                    SourceRef = req.SourceRef,
                    License = req.License,
                    Visible = req.Visible,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    ContentHash = Guid.NewGuid().ToString("N")
                };
                _db.Adhkar.Add(entity);
            }
            else
            {
                entity.Categories = req.Categories;
                entity.Priority = req.Priority;
                entity.Repetitions = req.Repetitions;
                entity.SourceBook = req.SourceBook;
                entity.SourceRef = req.SourceRef;
                entity.License = req.License;
                entity.Visible = req.Visible;
                entity.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _db.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessResponse(new { entity.Id, entity.Code }, "Adhkar upsert ok"));
        }

        // 2) AGGIUNGI/AGGIORNA TESTO LINGUA PER ADHKAR
        [HttpPost("{code}/texts")]
        public async Task<IActionResult> UpsertText([FromRoute] string code, [FromBody] UpsertAdhkarTextRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.ValidationErrorResponse(
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

            var adhkar = await _db.Adhkar.Include(a => a.Texts).FirstOrDefaultAsync(a => a.Code == code);
            if (adhkar is null) return NotFound(ApiResponse<object>.ErrorResponse("Adhkar non trovato"));

            var existing = adhkar.Texts.FirstOrDefault(t => t.Lang == req.Lang);
            if (existing is null)
            {
                existing = new AdhkarText
                {
                    AdhkarId = adhkar.Id,
                    Lang = req.Lang
                };
                adhkar.Texts.Add(existing);
            }
            existing.TextAr = req.TextAr;
            existing.Transliteration = req.Transliteration;
            existing.Translation = req.Translation;

            adhkar.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessResponse(null, "Testo upsert ok"));
        }

        // 3) CREA/AGGIORNA SET (morning/evening)
        [HttpPost("sets")]
        public async Task<IActionResult> UpsertSet([FromBody] CreateAdhkarSetRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.ValidationErrorResponse(
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

            var set = await _db.AdhkarSets.FirstOrDefaultAsync(s => s.Code == req.Code);
            if (set is null)
            {
                set = new AdhkarSet
                {
                    Id = Guid.NewGuid(),
                    Code = req.Code,
                    TitleIt = req.TitleIt,
                    Type = req.Type,
                    Ord = req.Ord,
                    EveningStart = req.EveningStart,
                    EveningEnd = req.EveningEnd
                };
                _db.AdhkarSets.Add(set);
            }
            else
            {
                set.TitleIt = req.TitleIt;
                set.Type = req.Type;
                set.Ord = req.Ord;
                set.EveningStart = req.EveningStart;
                set.EveningEnd = req.EveningEnd;
            }

            await _db.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessResponse(new { set.Id, set.Code }, "Set upsert ok"));
        }

        // 4) AGGIUNGI ITEM AL SET
        [HttpPost("sets/{setCode}/items")]
        public async Task<IActionResult> AddItem([FromRoute] string setCode, [FromBody] AddAdhkarSetItemRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.ValidationErrorResponse(
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

            var set = await _db.AdhkarSets.Include(s => s.Items).FirstOrDefaultAsync(s => s.Code == setCode);
            if (set is null) return NotFound(ApiResponse<object>.ErrorResponse("Set non trovato"));

            var adhkar = await _db.Adhkar.FirstOrDefaultAsync(a => a.Code == req.AdhkarCode);
            if (adhkar is null) return NotFound(ApiResponse<object>.ErrorResponse("Adhkar non trovato"));

            var exists = set.Items.FirstOrDefault(i => i.AdhkarId == adhkar.Id);
            if (exists is null)
            {
                exists = new AdhkarSetItem
                {
                    Id = Guid.NewGuid(),
                    SetId = set.Id,
                    AdhkarId = adhkar.Id
                };
                set.Items.Add(exists);
            }
            exists.Ord = req.Ord;
            exists.Repetitions = req.Repetitions;

            await _db.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessResponse(null, "Item aggiunto/aggiornato"));
        }
    }
}
