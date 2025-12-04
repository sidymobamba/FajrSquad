using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FajrSquad.Infrastructure.Services;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Core.DTOs;
using FajrSquad.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using System.Linq;

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
        private readonly IConfiguration _cfg;

        private const int DefaultRefreshDays = 60; // 2 mesi

        public ProfileController(
            IFileUploadService fileUploadService,
            FajrDbContext context,
            IFajrService fajrService,
            ILogger<ProfileController> logger,
            IMapper mapper,
            IConfiguration cfg)
        {
            _fileUploadService = fileUploadService;
            _context = context;
            _fajrService = fajrService;
            _logger = logger;
            _mapper = mapper;
            _cfg = cfg;
        }

        // ========= Helpers =========

        private bool TryGetUserId(out Guid userId)
        {
            userId = Guid.Empty;
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return !string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out userId);
        }

        private static string GenerateSecureToken(int bytes = 64)
        {
            var buf = new byte[bytes];
            RandomNumberGenerator.Fill(buf);
            return Convert.ToBase64String(buf).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }

        private int GetAccessMinutesFromConfig() =>
            _cfg.GetValue<int?>("Jwt:ExpirationMinutes") ?? 60;

        private int GetRefreshDaysFromConfig() =>
            _cfg.GetValue<int?>("Jwt:RefreshTokenDays") ?? DefaultRefreshDays;

        private AuthResponse BuildAuthResponse(User user, string accessToken, DateTime accessExpUtc, RefreshToken rt)
        {
            return new AuthResponse
            {
                AccessToken = accessToken,
                AccessTokenExpiresAt = accessExpUtc,
                ExpiresIn = (int)(accessExpUtc - DateTime.UtcNow).TotalSeconds,
                RefreshToken = rt.Token,
                RefreshTokenExpiresAt = rt.Expires,
                TokenType = "Bearer",
                User = new UserSummaryDto
                {
                    Id = user.Id.ToString(),
                    Name = user.Name,
                    Email = user.Email,
                    Phone = user.Phone,
                    City = user.City,
                    Country = user.Country,
                    Role = user.Role,
                    Avatar = user.ProfilePicture
                }
            };
        }

        private async Task<RefreshToken> CreateAndStoreRefreshAsync(User user)
        {
            var rt = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = GenerateSecureToken(64),
                Created = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(GetRefreshDaysFromConfig())
            };
            _context.RefreshTokens.Add(rt);
            await _context.SaveChangesAsync();
            return rt;
        }

        // ========= Avatar =========

        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

            var result = await _fileUploadService.UploadAvatarAsync(file, userId);
            if (!result.Success) 
            {
                var errorMessage = result.ErrorMessage ?? result.Errors?.FirstOrDefault() ?? "Errore sconosciuto";
                return BadRequest(ApiResponse<object>.ErrorResponse(errorMessage));
            }

            return Ok(ApiResponse<object>.SuccessResponse(result.Data!, "Avatar caricato con successo"));
        }

        [HttpPut("update-avatar")]
        public async Task<IActionResult> UpdateAvatar(IFormFile file)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

            var result = await _fileUploadService.UpdateAvatarAsync(file, userId);
            if (!result.Success) 
            {
                var errorMessage = result.ErrorMessage ?? result.Errors?.FirstOrDefault() ?? "Errore sconosciuto";
                return BadRequest(ApiResponse<object>.ErrorResponse(errorMessage));
            }

            return Ok(ApiResponse<object>.SuccessResponse(result.Data!, "Avatar aggiornato con successo"));
        }

        [HttpDelete("delete-avatar")]
        public async Task<IActionResult> DeleteAvatar()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

            var result = await _fileUploadService.DeleteAvatarAsync(userId);
            if (!result.Success) 
            {
                var errorMessage = result.ErrorMessage ?? result.Errors?.FirstOrDefault() ?? "Errore sconosciuto";
                return BadRequest(ApiResponse<object>.ErrorResponse(errorMessage));
            }

            return Ok(ApiResponse<bool>.SuccessResponse(result.Data, "Avatar eliminato con successo"));
        }

        [HttpGet("avatar")]
        public async Task<IActionResult> GetAvatar()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound(ApiResponse<object>.ErrorResponse("Utente non trovato"));

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                hasAvatar = !string.IsNullOrEmpty(user.ProfilePicture),
                avatarUrl = user.ProfilePicture
            }));
        }

        // ========= Profilo & Stats =========

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound(ApiResponse<object>.ErrorResponse("Utente non trovato"));

            var userSettings = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);
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
        public async Task<IActionResult> UpdateProfile(
            [FromBody] UpdateProfileRequest request,
            [FromServices] JwtService jwt)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound(ApiResponse<object>.ErrorResponse("Utente non trovato"));

            // Aggiorna i dati utente
            if (!string.IsNullOrWhiteSpace(request.Name)) user.Name = request.Name;
            if (!string.IsNullOrWhiteSpace(request.Email)) user.Email = request.Email;
            if (!string.IsNullOrWhiteSpace(request.City)) user.City = request.City;
            if (!string.IsNullOrWhiteSpace(request.Country)) user.Country = request.Country;
            
            // Aggiorna coordinate location se fornite
            if (request.Latitude.HasValue) user.Latitude = request.Latitude.Value;
            if (request.Longitude.HasValue) user.Longitude = request.Longitude.Value;
            if (!string.IsNullOrWhiteSpace(request.TimeZone)) user.TimeZone = request.TimeZone;

            await _context.SaveChangesAsync();

            // Log dettagliato per debug
            _logger.LogInformation(
                "Profile updated - UserId={UserId}, City={City}, Country={Country}, Lat={Lat}, Lng={Lng}, TZ={TZ}",
                userId, user.City, user.Country, user.Latitude, user.Longitude, user.TimeZone);

            // Rigenera i token con i dati aggiornati
            var accessMinutes = GetAccessMinutesFromConfig();
            var accessToken = jwt.GenerateAccessToken(user);
            var accessExpUtc = DateTime.UtcNow.AddMinutes(accessMinutes);
            
            // Ottieni il refresh token corrente (non revocato) o creane uno nuovo
            var currentRefresh = await _context.RefreshTokens
                .FirstOrDefaultAsync(r => r.UserId == userId && r.Revoked == null && r.Expires > DateTime.UtcNow);
            
            RefreshToken refreshToken;
            if (currentRefresh != null)
            {
                // Usa il refresh token esistente (non facciamo rotazione per semplicità)
                refreshToken = currentRefresh;
            }
            else
            {
                // Crea un nuovo refresh token se non ce n'è uno valido
                refreshToken = await CreateAndStoreRefreshAsync(user);
            }

            var authResponse = BuildAuthResponse(user, accessToken, accessExpUtc, refreshToken);

            // Restituisci sia i dati del profilo che i nuovi token
            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                user.Id,
                user.Name,
                user.Email,
                user.City,
                user.Country,
                user.Latitude,
                user.Longitude,
                user.TimeZone,
                user.ProfilePicture,
                // Includi i nuovi token nella risposta
                tokens = authResponse
            }, "Profilo aggiornato con successo"));
        }

        // ========= Cambio PIN con rotazione token =========

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordRequest request,
            [FromServices] JwtService jwt)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(ApiResponse<object>.ErrorResponse("Token non valido"));

            // Validazioni PIN 4 cifre
            if (request.OldPin?.Length != 4 || !request.OldPin.All(char.IsDigit))
                return BadRequest(ApiResponse<object>.ErrorResponse("Il vecchio PIN deve essere di 4 cifre numeriche."));
            if (request.NewPin?.Length != 4 || !request.NewPin.All(char.IsDigit))
                return BadRequest(ApiResponse<object>.ErrorResponse("Il nuovo PIN deve essere di 4 cifre numeriche."));

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound(ApiResponse<object>.ErrorResponse("Utente non trovato"));

            if (!BCrypt.Net.BCrypt.Verify(request.OldPin, user.PasswordHash))
                return BadRequest(ApiResponse<object>.ErrorResponse("PIN attuale non corretto"));

            // 1) aggiorna hash
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPin);
            await _context.SaveChangesAsync();

            // 2) revoca tutti i refresh token attivi
            var activeRefresh = await _context.RefreshTokens
                .Where(r => r.UserId == user.Id && r.Revoked == null && r.Expires > DateTime.UtcNow)
                .ToListAsync();
            foreach (var r in activeRefresh) r.Revoked = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // 3) emetti nuova coppia token per evitare logout lato app
            var accessMinutes = GetAccessMinutesFromConfig();
            var accessToken = jwt.GenerateAccessToken(user);
            var accessExpUtc = DateTime.UtcNow.AddMinutes(accessMinutes);
            var newRefresh = await CreateAndStoreRefreshAsync(user);
            var payload = BuildAuthResponse(user, accessToken, accessExpUtc, newRefresh);

            return Ok(ApiResponse<AuthResponse>.SuccessResponse(payload, "PIN cambiato con successo"));
        }
    }
}
