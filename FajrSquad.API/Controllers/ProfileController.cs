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

        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                var result = await _fileUploadService.UploadAvatarAsync(file, userId);

                if (!result.Success)
                    return BadRequest(ApiResponse<object>.ErrorResponse(result.ErrorMessage!));

                return Ok(ApiResponse<string>.SuccessResponse(result.Data!, "Avatar caricato con successo"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [HttpPut("update-avatar")]
        public async Task<IActionResult> UpdateAvatar(IFormFile file)
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                var result = await _fileUploadService.UpdateAvatarAsync(file, userId);

                if (!result.Success)
                    return BadRequest(ApiResponse<object>.ErrorResponse(result.ErrorMessage!));

                return Ok(ApiResponse<string>.SuccessResponse(result.Data!, "Avatar aggiornato con successo"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating avatar");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [HttpDelete("delete-avatar")]
        public async Task<IActionResult> DeleteAvatar()
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                var result = await _fileUploadService.DeleteAvatarAsync(userId);

                if (!result.Success)
                    return BadRequest(ApiResponse<object>.ErrorResponse(result.ErrorMessage!));

                return Ok(ApiResponse<bool>.SuccessResponse(result.Data, "Avatar eliminato con successo"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting avatar");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [HttpGet("avatar")]
        public async Task<IActionResult> GetAvatar()
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                var result = await _fileUploadService.GetAvatarUrlAsync(userId);

                if (!result.Success)
                    return BadRequest(ApiResponse<object>.ErrorResponse(result.ErrorMessage!));

                return Ok(ApiResponse<object>.SuccessResponse(new { 
                    hasAvatar = !string.IsNullOrEmpty(result.Data),
                    avatarUrl = result.Data 
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting avatar");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return NotFound(ApiResponse<object>.ErrorResponse("Utente non trovato"));

                _logger.LogInformation("👤 Utente trovato: {Name}", user.Name);

                var userSettings = await _context.UserSettings
                    .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);
                _logger.LogInformation("⚙️  UserSettings trovate: {Found}", userSettings != null);

                var statsResult = await _fajrService.GetUserStatsAsync(userId);
                _logger.LogInformation("📊 Stats: Success={Success}", statsResult.Success);

                var avatarResult = await _fileUploadService.GetAvatarUrlAsync(userId);
                _logger.LogInformation("🖼️ Avatar: Success={Success}", avatarResult.Success);

                var profileDto = new UserProfileDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Phone = user.Phone,
                    City = user.City,
                    Role = user.Role,
                    RegisteredAt = user.RegisteredAt,
                    ProfilePictureUrl = avatarResult.Success ? avatarResult.Data : null,
                    HasAvatar = avatarResult.Success && !string.IsNullOrEmpty(avatarResult.Data),
                    Stats = statsResult.Success ? statsResult.Data : null,
                    Settings = userSettings != null ? _mapper.Map<UserSettingsDto>(userSettings) : null
                };

                return Ok(ApiResponse<UserProfileDto>.SuccessResponse(profileDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero del profilo");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }



        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return NotFound(ApiResponse<object>.ErrorResponse("Utente non trovato"));

                // Update user fields
                if (!string.IsNullOrWhiteSpace(request.Name))
                    user.Name = request.Name;
                
                if (!string.IsNullOrWhiteSpace(request.Email))
                    user.Email = request.Email;
                
                if (!string.IsNullOrWhiteSpace(request.City))
                    user.City = request.City;
                
                // MotivatingBrother will be available after updating User entity
                // if (!string.IsNullOrWhiteSpace(request.MotivatingBrother))
                //     user.MotivatingBrother = request.MotivatingBrother;

                // UpdatedAt will be available after updating User entity to inherit from BaseEntity
                // user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse(new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    user.City
                    // MotivatingBrother will be available after updating User entity
                }, "Profilo aggiornato con successo"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return NotFound(ApiResponse<object>.ErrorResponse("Utente non trovato"));

                // Verify old password
                if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
                    return BadRequest(ApiResponse<object>.ErrorResponse("Password attuale non corretta"));

                // Update password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                // user.UpdatedAt = DateTime.UtcNow; // Will be available after updating User entity to inherit from BaseEntity

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse(null, "Password cambiata con successo"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                var settings = await _context.UserSettings
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (settings == null)
                {
                    // Create default settings
                    settings = new UserSettings
                    {
                        UserId = userId,
                        FajrReminder = true,
                        MorningHadith = true,
                        EveningMotivation = true,
                        IslamicHolidays = true,
                        FastingReminders = true,
                        SleepReminders = true,
                        Language = "fr",
                        Timezone = "Europe/Paris"
                    };

                    _context.UserSettings.Add(settings);
                    await _context.SaveChangesAsync();
                }

                return Ok(ApiResponse<UserSettings>.SuccessResponse(settings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user settings");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [HttpPut("settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateUserSettingsRequest request)
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                var settings = await _context.UserSettings
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (settings == null)
                {
                    settings = new UserSettings { UserId = userId };
                    _context.UserSettings.Add(settings);
                }

                // Update notification settings
                if (request.FajrReminder.HasValue)
                    settings.FajrReminder = request.FajrReminder.Value;
                
                if (request.MorningHadith.HasValue)
                    settings.MorningHadith = request.MorningHadith.Value;
                
                if (request.EveningMotivation.HasValue)
                    settings.EveningMotivation = request.EveningMotivation.Value;
                
                if (request.IslamicHolidays.HasValue)
                    settings.IslamicHolidays = request.IslamicHolidays.Value;
                
                if (request.FastingReminders.HasValue)
                    settings.FastingReminders = request.FastingReminders.Value;
                
                if (request.SleepReminders.HasValue)
                    settings.SleepReminders = request.SleepReminders.Value;

                // Update timing settings
                if (request.FajrReminderTime.HasValue)
                    settings.FajrReminderTime = request.FajrReminderTime.Value;
                
                if (request.MorningHadithTime.HasValue)
                    settings.MorningHadithTime = request.MorningHadithTime.Value;
                
                if (request.EveningMotivationTime.HasValue)
                    settings.EveningMotivationTime = request.EveningMotivationTime.Value;
                
                if (request.SleepReminderTime.HasValue)
                    settings.SleepReminderTime = request.SleepReminderTime.Value;

                // Update preferences
                if (!string.IsNullOrWhiteSpace(request.Language))
                    settings.Language = request.Language;
                
                if (!string.IsNullOrWhiteSpace(request.Timezone))
                    settings.Timezone = request.Timezone;
                
                if (request.DarkMode.HasValue)
                    settings.DarkMode = request.DarkMode.Value;
                
                if (request.SoundEnabled.HasValue)
                    settings.SoundEnabled = request.SoundEnabled.Value;
                
                if (request.VibrationEnabled.HasValue)
                    settings.VibrationEnabled = request.VibrationEnabled.Value;

                // Update privacy settings
                if (request.ShowInLeaderboard.HasValue)
                    settings.ShowInLeaderboard = request.ShowInLeaderboard.Value;
                
                if (request.AllowMotivatingBrotherNotifications.HasValue)
                    settings.AllowMotivatingBrotherNotifications = request.AllowMotivatingBrotherNotifications.Value;
                
                if (request.ShareStreakPublicly.HasValue)
                    settings.ShareStreakPublicly = request.ShareStreakPublicly.Value;

                // settings.UpdatedAt = DateTime.UtcNow; // Will be available after UserSettings inherits from BaseEntity
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<UserSettings>.SuccessResponse(settings, "Impostazioni aggiornate con successo"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user settings");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetDetailedStats()
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                var statsResult = await _fajrService.GetUserStatsAsync(userId);
                if (!statsResult.Success)
                    return BadRequest(ApiResponse<object>.ErrorResponse(statsResult.ErrorMessage!));

                // Get additional stats
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                var totalDays = (DateTime.UtcNow.Date - user!.RegisteredAt.Date).Days + 1;
                var checkInRate = statsResult.Data!.TotalCheckIns > 0 ? 
                    (double)statsResult.Data.TotalCheckIns / totalDays * 100 : 0;

                // Get weekly stats
                var startOfWeek = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
                var weeklyCheckIns = await _context.FajrCheckIns
                    .Where(f => f.UserId == userId && f.Date >= startOfWeek)
                    .CountAsync();

                // Get monthly stats
                var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var monthlyCheckIns = await _context.FajrCheckIns
                    .Where(f => f.UserId == userId && f.Date >= startOfMonth)
                    .CountAsync();

                var detailedStats = new
                {
                    Basic = statsResult.Data,
                    Additional = new
                    {
                        TotalDaysSinceRegistration = totalDays,
                        CheckInRate = Math.Round(checkInRate, 2),
                        WeeklyCheckIns = weeklyCheckIns,
                        MonthlyCheckIns = monthlyCheckIns,
                        RegistrationDate = user.RegisteredAt,
                        LastCheckIn = await _context.FajrCheckIns
                            .Where(f => f.UserId == userId)
                            .OrderByDescending(f => f.Date)
                            .Select(f => f.Date)
                            .FirstOrDefaultAsync()
                    }
                };

                return Ok(ApiResponse<object>.SuccessResponse(detailedStats));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting detailed stats");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        [HttpDelete("delete-account")]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return NotFound(ApiResponse<object>.ErrorResponse("Utente non trovato"));

                // Verify password for security
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                    return BadRequest(ApiResponse<object>.ErrorResponse("Password non corretta"));

                // Soft delete user and related data
                // Note: These properties will be available after updating User entity to inherit from BaseEntity
                // user.IsDeleted = true;
                // user.DeletedAt = DateTime.UtcNow;
                
                // For now, we'll do a hard delete until User entity is updated
                _context.Users.Remove(user);

                // Delete avatar file
                await _fileUploadService.DeleteAvatarAsync(userId);

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse(null, "Account eliminato con successo"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting account");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Errore interno del server"));
            }
        }

        private bool TryGetUserId(out Guid userId)
        {
            userId = Guid.Empty;
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            
            if (string.IsNullOrEmpty(userIdClaim))
                return false;

            return Guid.TryParse(userIdClaim, out userId);
        }
    }
}