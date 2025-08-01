using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
        private readonly string _uploadsPath;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB

        public FileUploadService(ILogger<FileUploadService> logger, IHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
            
            // Per IHostEnvironment, usiamo ContentRootPath e creiamo la struttura manualmente
            var webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
            _uploadsPath = Path.Combine(webRootPath, "uploads", "avatars");
            
            // Crea directory se non esiste (incluso wwwroot)
            Directory.CreateDirectory(_uploadsPath);
        }

        public async Task<ServiceResult<string>> UploadAvatarAsync(IFormFile file, Guid userId)
        {
            try
            {
                if (!IsValidImageFile(file))
                {
                    return ServiceResult<string>.ErrorResult("File non valido. Sono supportati solo JPG, PNG, GIF fino a 5MB");
                }

                // Elimina avatar esistente se presente
                await DeleteExistingAvatarAsync(userId);

                var fileName = $"{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(_uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var avatarUrl = $"/uploads/avatars/{fileName}";
                _logger.LogInformation("Avatar uploaded successfully for user {UserId}: {FileName}", userId, fileName);

                return ServiceResult<string>.SuccessResult(avatarUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar for user {UserId}", userId);
                return ServiceResult<string>.ErrorResult("Errore durante l'upload dell'avatar");
            }
        }

        public async Task<ServiceResult<string>> UpdateAvatarAsync(IFormFile file, Guid userId)
        {
            // Update è uguale a Upload (elimina vecchio e carica nuovo)
            return await UploadAvatarAsync(file, userId);
        }

        public async Task<ServiceResult<bool>> DeleteAvatarAsync(Guid userId)
        {
            try
            {
                var deleted = await DeleteExistingAvatarAsync(userId);
                return ServiceResult<bool>.SuccessResult(deleted);
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
                var files = Directory.GetFiles(_uploadsPath, $"{userId}_*");
                if (files.Length > 0)
                {
                    var fileName = Path.GetFileName(files[0]);
                    var avatarUrl = $"/uploads/avatars/{fileName}";
                    return ServiceResult<string>.SuccessResult(avatarUrl);
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
            if (file == null || file.Length == 0)
                return false;

            if (file.Length > _maxFileSize)
                return false;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return _allowedExtensions.Contains(extension);
        }

        private Task<bool> DeleteExistingAvatarAsync(Guid userId)
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