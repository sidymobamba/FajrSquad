using FajrSquad.Infrastructure.Data;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly StorageClient _storageClient;
        private readonly string _bucket;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB

        public FileUploadService(ILogger<FileUploadService> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;

            _bucket = Environment.GetEnvironmentVariable("FIREBASE_STORAGE_BUCKET")
                      ?? throw new InvalidOperationException("FIREBASE_STORAGE_BUCKET not set");

            var firebaseJson = Environment.GetEnvironmentVariable("FIREBASE_CONFIG_JSON")
                               ?? throw new InvalidOperationException("FIREBASE_CONFIG_JSON not set");

            var credential = GoogleCredential.FromJson(firebaseJson);
            _storageClient = StorageClient.Create(credential);
        }

        public async Task<ServiceResult<string>> UploadAvatarAsync(IFormFile file, Guid userId)
        {
            try
            {
                if (!IsValidImageFile(file))
                    return ServiceResult<string>.ErrorResult("File non valido. Sono supportati solo JPG, PNG, GIF fino a 5MB");

                // elimina eventuale avatar precedente
                await DeleteAvatarAsync(userId);

                var fileName = $"avatars/{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}";

                using var stream = file.OpenReadStream();
                await _storageClient.UploadObjectAsync(_bucket, fileName, file.ContentType, stream);

                // URL pubblico
                var avatarUrl = $"https://firebasestorage.googleapis.com/v0/b/{_bucket}/o/{Uri.EscapeDataString(fileName)}?alt=media";

                // aggiorna DB
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
                    // Ricava il path relativo da Firebase URL
                    var fileName = ExtractObjectNameFromUrl(user.ProfilePictureUrl);

                    if (!string.IsNullOrEmpty(fileName))
                    {
                        await _storageClient.DeleteObjectAsync(_bucket, fileName);
                        _logger.LogInformation("Deleted avatar from Firebase: {FileName}", fileName);
                    }

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

        private string ExtractObjectNameFromUrl(string url)
        {
            try
            {
                var start = url.IndexOf("/o/") + 3;
                var end = url.IndexOf("?alt=");
                var encoded = url[start..end];
                return Uri.UnescapeDataString(encoded);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
