using FajrSquad.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using FajrSquad.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using FajrSquad.Infrastructure.Services;
using FajrSquad.Core.DTOs;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace FajrSquad.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly FajrDbContext _db;

        public AuthController(FajrDbContext db)
        {
            _db = db;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request, [FromServices] JwtService jwt)
        {
            var exists = await _db.Users.AnyAsync(u => u.Phone == request.Phone);
            if (exists)
                return BadRequest("Telefono già registrato.");

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                City = request.City,
                PasswordHash = hashedPassword,
                Role = "User"
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var token = jwt.GenerateToken(user);
            return Ok(new { token, user.Id, user.Name, user.Email, user.City });
        }

        [HttpPost("register-with-otp")]
        public async Task<IActionResult> RegisterWithOtp(RegisterWithOtpRequest request, [FromServices] JwtService jwt)
        {
            // Verifica OTP
            var otpRecord = await _db.OtpCodes
                .FirstOrDefaultAsync(o => o.Phone == request.Phone && o.Code == request.Otp && !o.IsUsed);

            if (otpRecord == null || otpRecord.ExpiresAt < DateTime.UtcNow)
                return BadRequest("OTP non valido o scaduto.");

            var exists = await _db.Users.AnyAsync(u => u.Phone == request.Phone);
            if (exists)
                return BadRequest("Telefono già registrato.");

            // Marca OTP come usato
            otpRecord.IsUsed = true;

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                City = request.City,
                MotivatingBrother = request.MotivatingBrother,
                PasswordHash = "", // Non serve password con OTP
                Role = "User"
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var token = jwt.GenerateToken(user);
            return Ok(new { token, user.Id, user.Name, user.Email, user.City });
        }

        [HttpPost("login-with-otp")]
        public async Task<IActionResult> LoginWithOtp(LoginWithOtpRequest request, [FromServices] JwtService jwt)
        {
            // Verifica OTP
            var otpRecord = await _db.OtpCodes
                .FirstOrDefaultAsync(o => o.Phone == request.Phone && o.Code == request.Otp && !o.IsUsed);

            if (otpRecord == null || otpRecord.ExpiresAt < DateTime.UtcNow)
                return BadRequest("OTP non valido o scaduto.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Phone == request.Phone);
            if (user == null)
                return Unauthorized("Utente non trovato.");

            // Marca OTP come usato
            otpRecord.IsUsed = true;
            await _db.SaveChangesAsync();

            var token = jwt.GenerateToken(user);
            return Ok(new { token, user.Id, user.Name, user.Email, user.City });
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _db.Users
                .Select(u => new { u.Id, u.Name, u.Email, u.Phone, u.City, u.Role })
                .ToListAsync();
            return Ok(users);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request, [FromServices] JwtService jwt)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Phone == request.Phone);
            if (user == null)
                return Unauthorized("Utente non trovato.");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized("Password errata.");

            var token = jwt.GenerateToken(user);
            return Ok(new { token, user.Id, user.Name, user.Email, user.City });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (userId == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.OldPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest("Vecchia e nuova password sono richieste.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);
            if (user == null)
                return Unauthorized();

            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
                return BadRequest("La vecchia password non è corretta.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            return Ok("Password cambiata con successo.");
        }

        [Authorize]
        [HttpGet("/api/user/me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (userId == null)
                return Unauthorized();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);
            if (user == null)
                return NotFound("Utente non trovato.");

            return Ok(new
            {
                user.Id,
                user.Name,
                user.Email,
                user.Phone,
                user.City,
                user.Role
            });
        }

        [Authorize]
        [HttpPut("/api/user")]
        public async Task<IActionResult> UpdateOwnProfile(UpdateUserRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (userId == null)
                return Unauthorized();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);
            if (user == null)
                return NotFound("Utente non trovato.");

            user.Name = request.Name ?? user.Name;
            user.City = request.City ?? user.City;
            user.Email = request.Email ?? user.Email;

            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            return Ok(new { user.Id, user.Name, user.Email, user.City });
        }

        [Authorize]
        [HttpDelete("/api/user")]
        public async Task<IActionResult> DeleteOwnAccount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (userId == null)
                return Unauthorized();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);
            if (user == null)
                return NotFound("Utente non trovato.");

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            return Ok("Account eliminato con successo.");
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("/api/admin/users/{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, UpdateUserRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return NotFound("Utente non trovato.");

            user.Name = request.Name ?? user.Name;
            user.City = request.City ?? user.City;
            user.Email = request.Email ?? user.Email;
            user.Role = request.Role ?? user.Role;

            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            return Ok(new { user.Id, user.Name, user.Email, user.City, user.Role });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("/api/admin/users/{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return NotFound("Utente non trovato.");

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
            var checkedInUserIds = await _db.FajrCheckIns.Where(f => f.Date == today).Select(f => f.UserId).ToListAsync();
            var missedUsers = allUsers.Where(u => !checkedInUserIds.Contains(u.Id)).ToList();

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
            if (existing != null)
                existing.Token = request.Token;
            else
                _db.DeviceTokens.Add(new DeviceToken { UserId = userId, Token = request.Token });

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

