using System.Security.Claims;
using System.Security.Cryptography;
using FajrSquad.Core.DTOs;
using FajrSquad.Core.Entities;
using FajrSquad.Infrastructure.Data;
using FajrSquad.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FajrSquad.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly FajrDbContext _db;
        private readonly IConfiguration _cfg;
        private const int DefaultRefreshDays = 60; // 2 mesi

        public AuthController(FajrDbContext db, IConfiguration cfg)
        {
            _db = db;
            _cfg = cfg;
        }

        // ========= HELPERS =========

        private static string GenerateSecureToken(int bytes = 64)
        {
            var buf = new byte[bytes];
            RandomNumberGenerator.Fill(buf);
            // Base64Url
            return Convert.ToBase64String(buf)
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');
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

        private async Task<RefreshToken> CreateAndStoreRefreshAsync(User user, string? replacedBy = null)
        {
            var refresh = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = GenerateSecureToken(64),
                Created = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(GetRefreshDaysFromConfig()),
                ReplacedByToken = replacedBy
            };

            _db.RefreshTokens.Add(refresh);
            await _db.SaveChangesAsync();
            return refresh;
        }

        private async Task<AuthResponse> IssueTokensAsync(User user, JwtService jwt)
        {
            var accessMinutes = GetAccessMinutesFromConfig();
            var accessToken = jwt.GenerateAccessToken(user);
            var accessExpUtc = DateTime.UtcNow.AddMinutes(accessMinutes);

            var refresh = await CreateAndStoreRefreshAsync(user);

            return BuildAuthResponse(user, accessToken, accessExpUtc, refresh);
        }

        // ========= ENDPOINTS =========

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request, [FromServices] JwtService jwt)
        {
            var exists = await _db.Users.AnyAsync(u => u.Phone == request.Phone);
            if (exists) return BadRequest("Telefono già registrato.");

            if (request.Pin.Length != 4 || !request.Pin.All(char.IsDigit))
                return BadRequest("Il PIN deve essere di 4 cifre numeriche.");

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Pin);

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                City = request.City,
                Country = request.Country, // 👈 SALVATO
                PasswordHash = hashedPassword,
                Role = "User"
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var payload = await IssueTokensAsync(user, jwt);
            return Ok(payload);
        }

        [HttpPost("register-with-otp")]
        public async Task<IActionResult> RegisterWithOtp(RegisterWithOtpRequest request, [FromServices] JwtService jwt)
        {
            var otpRecord = await _db.OtpCodes
                .FirstOrDefaultAsync(o => o.Phone == request.Phone && o.Code == request.Otp && !o.IsUsed);

            if (otpRecord == null || otpRecord.ExpiresAt < DateTime.UtcNow)
                return BadRequest("OTP non valido o scaduto.");

            if (await _db.Users.AnyAsync(u => u.Phone == request.Phone))
                return BadRequest("Telefono già registrato.");

            otpRecord.IsUsed = true;

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                City = request.City,
                MotivatingBrother = request.MotivatingBrother,
                PasswordHash = "",
                Role = "User"
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var payload = await IssueTokensAsync(user, jwt);
            return Ok(payload);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request, [FromServices] JwtService jwt)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Phone == request.Phone);
            if (user == null) return Unauthorized("Utente non trovato.");

            if (!BCrypt.Net.BCrypt.Verify(request.Pin, user.PasswordHash))
                return Unauthorized("PIN errato.");

            var payload = await IssueTokensAsync(user, jwt);
            return Ok(payload);
        }

        [HttpPost("login-with-otp")]
        public async Task<IActionResult> LoginWithOtp(LoginWithOtpRequest request, [FromServices] JwtService jwt)
        {
            var otpRecord = await _db.OtpCodes
                .FirstOrDefaultAsync(o => o.Phone == request.Phone && o.Code == request.Otp && !o.IsUsed);

            if (otpRecord == null || otpRecord.ExpiresAt < DateTime.UtcNow)
                return BadRequest("OTP non valido o scaduto.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Phone == request.Phone);
            if (user == null) return Unauthorized("Utente non trovato.");

            otpRecord.IsUsed = true;
            await _db.SaveChangesAsync();

            var payload = await IssueTokensAsync(user, jwt);
            return Ok(payload);
        }

        // 🔄 Refresh Access Token (rotazione refresh token)
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest req, [FromServices] JwtService jwt)
        {
            if (string.IsNullOrWhiteSpace(req.RefreshToken))
                return BadRequest("Refresh token mancante.");

            var stored = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == req.RefreshToken);
            if (stored == null) return Unauthorized("Refresh token non valido.");
            if (stored.Revoked != null) return Unauthorized("Refresh token revocato.");
            if (stored.Expires <= DateTime.UtcNow) return Unauthorized("Refresh token scaduto.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == stored.UserId);
            if (user == null) return Unauthorized("Utente non valido.");

            // Rotazione: revoca il vecchio e crea il nuovo
            stored.Revoked = DateTime.UtcNow;
            var newRefresh = await CreateAndStoreRefreshAsync(user, replacedBy: null);
            stored.ReplacedByToken = newRefresh.Token;
            await _db.SaveChangesAsync();

            var accessMinutes = GetAccessMinutesFromConfig();
            var accessToken = jwt.GenerateAccessToken(user);
            var accessExpUtc = DateTime.UtcNow.AddMinutes(accessMinutes);

            var payload = BuildAuthResponse(user, accessToken, accessExpUtc, newRefresh);
            return Ok(payload);
        }

        // 🚪 Logout: revoca 1 token (se passato) o tutti i refresh dell’utente corrente
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RevokeRefreshTokenRequest req)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

            if (!string.IsNullOrWhiteSpace(req.RefreshToken))
            {
                var tok = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == req.RefreshToken && r.UserId == userId);
                if (tok != null && tok.Revoked == null) tok.Revoked = DateTime.UtcNow;
            }
            else
            {
                var all = await _db.RefreshTokens.Where(r => r.UserId == userId && r.Revoked == null).ToListAsync();
                foreach (var t in all) t.Revoked = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = "Logout eseguito." });
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _db.Users
                .Select(u => new { u.Id, u.Name, u.Email, u.Phone, u.City, u.Country, u.Role }) // 👈
                .ToListAsync();
            return Ok(users);
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (userId == null) return Unauthorized();

            if (request.OldPin?.Length != 4 || !request.OldPin.All(char.IsDigit))
                return BadRequest("Il vecchio PIN deve essere di 4 cifre numeriche.");

            if (request.NewPin?.Length != 4 || !request.NewPin.All(char.IsDigit))
                return BadRequest("Il nuovo PIN deve essere di 4 cifre numeriche.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);
            if (user == null) return Unauthorized();

            if (!BCrypt.Net.BCrypt.Verify(request.OldPin, user.PasswordHash))
                return BadRequest("Il vecchio PIN non è corretto.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPin);
            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            // (opzionale) revoca i refresh token attuali per sicurezza
            var tokens = await _db.RefreshTokens.Where(r => r.UserId == user.Id && r.Revoked == null).ToListAsync();
            foreach (var t in tokens) t.Revoked = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok("PIN cambiato con successo.");
        }

        [Authorize]
        [HttpGet("/api/user/me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (userId == null) return Unauthorized();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);
            if (user == null) return NotFound("Utente non trovato.");

            return Ok(new
            {
                user.Id,
                user.Name,
                user.Email,
                user.Phone,
                user.City,
                user.Country, 
                user.Role,
                user.ProfilePicture
            });
        }

        [Authorize]
        [HttpPut("/api/user")]
        public async Task<IActionResult> UpdateOwnProfile(UpdateUserRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (userId == null) return Unauthorized();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);
            if (user == null) return NotFound("Utente non trovato.");

            user.Name = request.Name ?? user.Name;
            user.City = request.City ?? user.City;
            user.Email = request.Email ?? user.Email;
            if (!string.IsNullOrWhiteSpace(request.Country)) user.Country = request.Country; 

            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            return Ok(new { user.Id, user.Name, user.Email, user.City, user.Country });
        }


        [Authorize]
        [HttpDelete("/api/user")]
        public async Task<IActionResult> DeleteOwnAccount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (userId == null) return Unauthorized();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);
            if (user == null) return NotFound("Utente non trovato.");

            // Revoca refresh prima di cancellare
            var tokens = await _db.RefreshTokens.Where(r => r.UserId == user.Id).ToListAsync();
            _db.RefreshTokens.RemoveRange(tokens);

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            return Ok("Account eliminato con successo.");
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("/api/admin/users/{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, UpdateUserRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound("Utente non trovato.");

            user.Name = request.Name ?? user.Name;
            user.City = request.City ?? user.City;
            user.Email = request.Email ?? user.Email;
            if (!string.IsNullOrWhiteSpace(request.Country)) user.Country = request.Country; 
            user.Role = request.Role ?? user.Role;

            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            return Ok(new { user.Id, user.Name, user.Email, user.City, user.Country, user.Role });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("/api/admin/users/{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound("Utente non trovato.");

            var tokens = await _db.RefreshTokens.Where(r => r.UserId == user.Id).ToListAsync();
            _db.RefreshTokens.RemoveRange(tokens);

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            return Ok("Utente eliminato con successo.");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("send-missed-reminder")]
        public async Task<IActionResult> SendMissedCheckinReminder()
        {
            var today = DateTime.UtcNow.Date;
            var allUsers = await _db.Users.ToListAsync();
            var checkedInUserIds = await _db.FajrCheckIns
                .Where(f => f.Date == today)
                .Select(f => f.UserId)
                .ToListAsync();

            var missedUsers = allUsers.Where(u => !checkedInUserIds.Contains(u.Id)).ToList();

            // TODO: invio reale push/SMS
            foreach (var user in missedUsers)
            {
                Console.WriteLine($"Reminder inviato a: {user.Name} - {user.Phone}");
            }

            return Ok(new { count = missedUsers.Count });
        }

        [Authorize]
        [HttpPost("device-token")]
        public async Task<IActionResult> SaveDeviceToken([FromBody] DeviceTokenDto request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var existing = await _db.DeviceTokens.FirstOrDefaultAsync(t => t.UserId == userId);
            if (existing != null) existing.Token = request.Token;
            else _db.DeviceTokens.Add(new DeviceToken { UserId = userId, Token = request.Token });

            await _db.SaveChangesAsync();
            return Ok("Token salvato.");
        }

        [Authorize]
        [HttpPost("report-problem")]
        public async Task<IActionResult> ReportProblem([FromBody] ProblemReportDto report)
        {
            _db.ProblemReports.Add(new ProblemReport
            {
                Id = Guid.NewGuid(),
                UserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
                Message = report.Message,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return Ok("Segnalazione ricevuta.");
        }
    }
}
