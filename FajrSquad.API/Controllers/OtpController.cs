using Microsoft.AspNetCore.Mvc;
using FajrSquad.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using FajrSquad.Core.Entities;
using FajrSquad.Infrastructure.Services;
using FajrSquad.Core.DTOs;

namespace FajrSquad.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OtpController : ControllerBase
    {
        private readonly FajrDbContext _db;

        public OtpController(FajrDbContext db)
        {
            _db = db;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
        {
            // Genera OTP di 6 cifre
            var otp = new Random().Next(100000, 999999).ToString();
            var expiresAt = DateTime.UtcNow.AddMinutes(5); // Scade in 5 minuti

            // Salva OTP nel database
            var otpRecord = new OtpCode
            {
                Id = Guid.NewGuid(),
                Phone = request.Phone,
                Code = otp,
                ExpiresAt = expiresAt,
                IsUsed = false
            };

            _db.OtpCodes.Add(otpRecord);
            await _db.SaveChangesAsync();

            // In produzione: inviare SMS tramite Twilio/Firebase
            // Per ora simuliamo l'invio
            Console.WriteLine($"OTP per {request.Phone}: {otp}");

            return Ok(new { message = "OTP inviato", phone = request.Phone });
        }
    }
}