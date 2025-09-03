using FajrSquad.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FajrSquad.Infrastructure.Services
{
    public interface IFileUploadService
    {
        Task<ServiceResult<string>> UploadAvatarAsync(IFormFile file, Guid userId);
        Task<ServiceResult<string>> UpdateAvatarAsync(IFormFile file, Guid userId);
        Task<ServiceResult<bool>> DeleteAvatarAsync(Guid userId);
        Task<ServiceResult<string>> GetAvatarUrlAsync(Guid userId);
        bool IsValidImageFile(IFormFile file);
    }

    public class FileUploadService : IFileUploadService
    {
        private readonly ILogger<FileUploadService> _logger;
        private readonly IHostEnvironment _environment;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly string _uploadsPath;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB

        public FileUploadService(
            ILogger<FileUploadService> logger,
            IHostEnvironment environment,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _environment = environment;
            _serviceScopeFactory = serviceScopeFactory;

            // Creazione percorso wwwroot/uploads/avatars
            var webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
            _uploadsPath = Path.Combine(webRootPath, "uploads", "avatars");
            Directory.CreateDirectory(_uploadsPath);
        }

        public async Task<ServiceResult<string>> UploadAvatarAsync(IFormFile file, Guid userId)
        {
            try
            {
                if (!IsValidImageFile(file))
                    return ServiceResult<string>.ErrorResult("File non valido. Sono supportati solo JPG, PNG, GIF fino a 5MB");

                // elimina vecchio avatar se presente
                await DeleteExistingAvatarFileAsync(userId);

                var fileName = $"{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(_uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var avatarUrl = $"/uploads/avatars/{fileName}";

                // 🔑 aggiorna il DB
                using var scope = _serviceScopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<FajrDbContext>();
                var user = await db.Users.FindAsync(userId);
                if (user != null)
                {
                    user.ProfilePictureUrl = avatarUrl;
                    await db.SaveChangesAsync();
                }

                return ServiceResult<string>.SuccessResult(avatarUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar for user {UserId}", userId);
                return ServiceResult<string>.ErrorResult("Errore durante l'upload dell'avatar");
            }
        }

        public Task<ServiceResult<string>> UpdateAvatarAsync(IFormFile file, Guid userId)
        {
            // Update = stesso comportamento di Upload
            return UploadAvatarAsync(file, userId);
        }

        public async Task<ServiceResult<bool>> DeleteAvatarAsync(Guid userId)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<FajrDbContext>();
                var user = await db.Users.FindAsync(userId);

                if (user != null && !string.IsNullOrEmpty(user.ProfilePictureUrl))
                {
                    // elimina file fisico
                    var filePath = Path.Combine(_environment.ContentRootPath, "wwwroot", user.ProfilePictureUrl.TrimStart('/'));
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        _logger.LogInformation("Deleted avatar file: {FileName}", filePath);
                    }

                    // reset campo DB
                    user.ProfilePictureUrl = null;
                    await db.SaveChangesAsync();
                }

                return ServiceResult<bool>.SuccessResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting avatar for user {UserId}", userId);
                return ServiceResult<bool>.ErrorResult("Errore durante l'eliminazione dell'avatar");
            }
        }

        public async Task<ServiceResult<string>> GetAvatarUrlAsync(Guid userId)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<FajrDbContext>();
                var user = await db.Users.FindAsync(userId);

                if (user != null && !string.IsNullOrEmpty(user.ProfilePictureUrl))
                {
                    return ServiceResult<string>.SuccessResult(user.ProfilePictureUrl);
                }

                return ServiceResult<string>.SuccessResult(string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting avatar URL for user {UserId}", userId);
                return ServiceResult<string>.ErrorResult("Errore nel recupero dell'avatar");
            }
        }

        public bool IsValidImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0) return false;
            if (file.Length > _maxFileSize) return false;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return _allowedExtensions.Contains(extension);
        }

        // 🔒 elimina solo il file vecchio (senza toccare DB)
        private Task<bool> DeleteExistingAvatarFileAsync(Guid userId)
        {
            var files = Directory.GetFiles(_uploadsPath, $"{userId}_*");
            foreach (var file in files)
            {
                File.Delete(file);
                _logger.LogInformation("Deleted existing avatar: {FileName}", Path.GetFileName(file));
            }
            return Task.FromResult(files.Length > 0);
        }
    }
}
