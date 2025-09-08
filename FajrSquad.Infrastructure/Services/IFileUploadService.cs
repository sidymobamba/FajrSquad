using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using FajrSquad.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FajrSquad.Infrastructure.Services
{
    public interface IFileUploadService
    {
        Task<ServiceResult<object>> UploadAvatarAsync(IFormFile file, Guid userId);
        Task<ServiceResult<object>> UpdateAvatarAsync(IFormFile file, Guid userId);
        Task<ServiceResult<bool>> DeleteAvatarAsync(Guid userId);
        Task<ServiceResult<string>> GetAvatarUrlAsync(Guid userId);
        bool IsValidImageFile(IFormFile file);

        string GeneratePreSignedUrl(string key, int minutes = 15);
    }

    public class FileUploadService : IFileUploadService
    {
        private readonly ILogger<FileUploadService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IAmazonS3 _s3;
        private readonly string _bucket;
        private readonly string _publicBaseUrl;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB

        public FileUploadService(ILogger<FileUploadService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;

            var accountId = Environment.GetEnvironmentVariable("R2_ACCOUNT_ID")
                            ?? throw new InvalidOperationException("R2_ACCOUNT_ID not set");
            var accessKey = Environment.GetEnvironmentVariable("R2_ACCESS_KEY_ID")
                            ?? throw new InvalidOperationException("R2_ACCESS_KEY_ID not set");
            var secretKey = Environment.GetEnvironmentVariable("R2_SECRET_ACCESS_KEY")
                            ?? throw new InvalidOperationException("R2_SECRET_ACCESS_KEY not set");

            _bucket = Environment.GetEnvironmentVariable("R2_BUCKET_NAME")
                      ?? throw new InvalidOperationException("R2_BUCKET_NAME not set");
            _publicBaseUrl = Environment.GetEnvironmentVariable("R2_PUBLIC_URL")
                             ?? throw new InvalidOperationException("R2_PUBLIC_URL not set");

            var cfg = new AmazonS3Config
            {
                ServiceURL = $"https://{accountId}.eu.r2.cloudflarestorage.com",
                ForcePathStyle = true,
                SignatureVersion = "v4"
            };

            _s3 = new AmazonS3Client(accessKey, secretKey, cfg);
        }

        public async Task<ServiceResult<object>> UploadAvatarAsync(IFormFile file, Guid userId)
        {
            try
            {
                if (!IsValidImageFile(file))
                    return ServiceResult<object>.ErrorResult("File non valido. Solo JPG/PNG/GIF fino a 5MB");

                await DeleteAvatarAsync(userId);

                var key = $"avatars/{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}";

                var request = new GetPreSignedUrlRequest
                {
                    BucketName = _bucket,
                    Key = key,
                    Verb = HttpVerb.PUT,
                    Expires = DateTime.UtcNow.AddMinutes(15),
                    ContentType = file.ContentType
                };
                var uploadUrl = _s3.GetPreSignedURL(request);

                var publicUrl = $"{_publicBaseUrl}/{key}";

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<FajrDbContext>();
                var user = await db.Users.FindAsync(userId);
                if (user != null)
                {
                    user.ProfilePictureUrl = publicUrl;
                    await db.SaveChangesAsync();
                }

                return ServiceResult<object>.SuccessResult(new
                {
                    uploadUrl,
                    publicUrl
                }, "Avatar caricato con successo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "S3 error uploading avatar for {UserId}", userId);
                return ServiceResult<object>.ErrorResult("Errore upload avatar");
            }
        }

        public Task<ServiceResult<object>> UpdateAvatarAsync(IFormFile file, Guid userId)
            => UploadAvatarAsync(file, userId);

        public async Task<ServiceResult<bool>> DeleteAvatarAsync(Guid userId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<FajrDbContext>();
                var user = await db.Users.FindAsync(userId);

                if (user != null && !string.IsNullOrEmpty(user.ProfilePictureUrl))
                {
                    var key = user.ProfilePictureUrl.Replace(_publicBaseUrl + "/", "");
                    await _s3.DeleteObjectAsync(_bucket, key);
                    user.ProfilePictureUrl = null;
                    await db.SaveChangesAsync();
                }

                return ServiceResult<bool>.SuccessResult(true, "Avatar eliminato con successo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting avatar");
                return ServiceResult<bool>.ErrorResult("Errore eliminazione avatar");
            }
        }

        public async Task<ServiceResult<string>> GetAvatarUrlAsync(Guid userId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FajrDbContext>();
            var user = await db.Users.FindAsync(userId);
            return ServiceResult<string>.SuccessResult(user?.ProfilePictureUrl ?? string.Empty, "Avatar recuperato");
        }

        public bool IsValidImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0) return false;
            if (file.Length > _maxFileSize) return false;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            return _allowedExtensions.Contains(ext);
        }

        public string GeneratePreSignedUrl(string key, int minutes = 15)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucket,
                Key = key,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(minutes),
                ContentType = "image/jpeg"
            };

            var url = _s3.GetPreSignedURL(request);
            _logger.LogInformation("✅ PreSigned URL generato: {Url}", url);
            return url;
        }
    }
}
