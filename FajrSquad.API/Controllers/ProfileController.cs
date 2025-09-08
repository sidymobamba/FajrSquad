using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FajrSquad.Infrastructure.Services;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Core.DTOs;
using FajrSquad.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BCrypt.Net;
using AutoMapper;

namespace FajrSquad.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IFileUploadService _fileUploadService;
        private readonly FajrDbContext _context;
        private readonly IFajrService _fajrService;
        private readonly ILogger<ProfileController> _logger;
        private readonly IMapper _mapper;

        public ProfileController(
            IFileUploadService fileUploadService,
            FajrDbContext context,
            IFajrService fajrService,
            ILogger<ProfileController> logger,
            IMapper mapper)
        {
            _fileUploadService = fileUploadService;
            _context = context;
            _fajrService = fajrService;
            _logger = logger;
            _mapper = mapper;
        }

        //[HttpGet("test-presign")]
        //public IActionResult TestPresign()
        //{
        //    var key = $"avatars/test_{Guid.NewGuid()}.jpg";
        //    var url = _fileUploadService.GeneratePreSignedUrl(key);
        //    return Ok(new { uploadUrl = url });
        //}

        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

            var result = await _fileUploadService.UploadAvatarAsync(file, userId);

            if (!result.Success)
                return BadRequest(ApiResponse<object>.ErrorResponse(result.ErrorMessage!));

            return Ok(ApiResponse<object>.SuccessResponse(result.Data!, "Avatar caricato con successo"));
        }

        [HttpPut("update-avatar")]
        public async Task<IActionResult> UpdateAvatar(IFormFile file)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

            var result = await _fileUploadService.UpdateAvatarAsync(file, userId);

            if (!result.Success)
                return BadRequest(ApiResponse<object>.ErrorResponse(result.ErrorMessage!));

            return Ok(ApiResponse<object>.SuccessResponse(result.Data!, "Avatar aggiornato con successo"));
        }

        [HttpDelete("delete-avatar")]
        public async Task<IActionResult> DeleteAvatar()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

            var result = await _fileUploadService.DeleteAvatarAsync(userId);

            if (!result.Success)
                return BadRequest(ApiResponse<object>.ErrorResponse(result.ErrorMessage!));

            return Ok(ApiResponse<bool>.SuccessResponse(result.Data, "Avatar eliminato con successo"));
        }

        [HttpGet("avatar")]
        public async Task<IActionResult> GetAvatar()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Utente non trovato"));

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                hasAvatar = !string.IsNullOrEmpty(user.ProfilePicture),
                avatarUrl = user.ProfilePicture
            }));
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Utente non trovato"));

            var userSettings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);

            var statsResult = await _fajrService.GetUserStatsAsync(userId);

            var profileDto = new UserProfileDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                City = user.City,
                Role = user.Role,
                RegisteredAt = user.RegisteredAt,
                ProfilePicture = user.ProfilePicture,
                HasAvatar = !string.IsNullOrEmpty(user.ProfilePicture),
                Stats = statsResult.Success ? statsResult.Data : null,
                Settings = userSettings != null ? _mapper.Map<UserSettingsDto>(userSettings) : null
            };

            return Ok(ApiResponse<UserProfileDto>.SuccessResponse(profileDto));
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Utente non trovato"));

            if (!string.IsNullOrWhiteSpace(request.Name))
                user.Name = request.Name;

            if (!string.IsNullOrWhiteSpace(request.Email))
                user.Email = request.Email;

            if (!string.IsNullOrWhiteSpace(request.City))
                user.City = request.City;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                user.Id,
                user.Name,
                user.Email,
                user.City,
                user.ProfilePicture
            }, "Profilo aggiornato con successo"));
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Utente non trovato"));

            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
                return BadRequest(ApiResponse<object>.ErrorResponse("Password attuale non corretta"));

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse(null, "Password cambiata con successo"));
        }

        // ⚡ Settings, Stats e DeleteAccount li lascio invariati (già corretti)

        private bool TryGetUserId(out Guid userId)
        {
            userId = Guid.Empty;
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

            return !string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out userId);
        }
    }
}
