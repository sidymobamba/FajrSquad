using FajrSquad.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;

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
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB

        public FileUploadService(ILogger<FileUploadService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task<ServiceResult<object>> UploadAvatarAsync(IFormFile file, Guid userId)
        {
            try
            {
                _logger.LogInformation("[UploadAvatarAsync] Start upload avatar per UserId={UserId}. FileName={FileName}, ContentType={ContentType}, Size={Size}", userId, file?.FileName, file?.ContentType, file?.Length);
                
                if (file == null || file.Length == 0)
                    return ServiceResult<object>.ErrorResult("Nessun file fornito o file vuoto");
                
                if (file.Length > _maxFileSize)
                    return ServiceResult<object>.ErrorResult($"File troppo grande: {file.Length / 1024 / 1024:F1}MB (massimo 5MB)");
                
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_allowedExtensions.Contains(ext))
                    return ServiceResult<object>.ErrorResult($"Formato file non supportato: {ext}. Formati supportati: {string.Join(", ", _allowedExtensions)}");
                
                if (!IsValidImageFile(file))
                    return ServiceResult<object>.ErrorResult("File non valido. Solo JPG/PNG/GIF fino a 5MB");

                _logger.LogInformation("[UploadAvatarAsync] Inizio eliminazione avatar precedente per UserId={UserId}", userId);
                await DeleteAvatarAsync(userId);
                _logger.LogInformation("[UploadAvatarAsync] Eliminazione avatar precedente completata per UserId={UserId}", userId);

                _logger.LogInformation("[UploadAvatarAsync] Inizio conversione file in Base64 per UserId={UserId}", userId);
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var bytes = ms.ToArray();
                _logger.LogInformation("[UploadAvatarAsync] Bytes letti: {BytesLength}", bytes.Length);
                
                var base64 = Convert.ToBase64String(bytes);
                var dataUrl = $"data:{file.ContentType};base64,{base64}";
                var previewLen = Math.Min(dataUrl.Length, 200);
                var preview = dataUrl.Substring(0, previewLen);
                _logger.LogInformation("[UploadAvatarAsync] Base64 preview (primi {PreviewLen} chars): {Preview}", previewLen, preview);
                _logger.LogDebug("[UploadAvatarAsync] Base64 completo (len={Len})", dataUrl.Length);
                _logger.LogInformation("[UploadAvatarAsync] Conversione Base64 completata per UserId={UserId}", userId);

                _logger.LogInformation("[UploadAvatarAsync] Inizio salvataggio in database per UserId={UserId}", userId);
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<FajrDbContext>();
                _logger.LogInformation("[UploadAvatarAsync] Database context ottenuto per UserId={UserId}", userId);
                
                var user = await db.Users.FindAsync(userId);
                _logger.LogInformation("[UploadAvatarAsync] Utente trovato: {UserFound} per UserId={UserId}", user != null, userId);
                
                if (user != null)
                {
                    _logger.LogInformation("[UploadAvatarAsync] Salvataggio Base64 in DB per UserId={UserId}", userId);
                    user.ProfilePicture = dataUrl;
                    _logger.LogInformation("[UploadAvatarAsync] ProfilePicture impostato, inizio SaveChanges per UserId={UserId}", userId);
                    await db.SaveChangesAsync();
                    _logger.LogInformation("[UploadAvatarAsync] Salvataggio completato per UserId={UserId}", userId);
                }
                else
                {
                    _logger.LogWarning("[UploadAvatarAsync] Utente non trovato per UserId={UserId}", userId);
                }

                return ServiceResult<object>.SuccessResult(new
                {
                    profilePicture = user?.ProfilePicture
                }, "Avatar caricato con successo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving base64 avatar for {UserId}. FileName={FileName}, ContentType={ContentType}, Size={Size}", 
                    userId, file?.FileName, file?.ContentType, file?.Length);
                return ServiceResult<object>.ErrorResult($"Errore upload avatar: {ex.Message}");
            }
        }

        public Task<ServiceResult<object>> UpdateAvatarAsync(IFormFile file, Guid userId)
        {
            _logger.LogInformation("[UpdateAvatarAsync] Update avatar richiesto per UserId={UserId}", userId);
            return UploadAvatarAsync(file, userId);
        }

        public async Task<ServiceResult<bool>> DeleteAvatarAsync(Guid userId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<FajrDbContext>();
                var user = await db.Users.FindAsync(userId);

                if (user != null && !string.IsNullOrEmpty(user.ProfilePicture))
                {
                    _logger.LogInformation("[DeleteAvatarAsync] Eliminazione avatar per UserId={UserId}", userId);
                    user.ProfilePicture = null;
                    await db.SaveChangesAsync();
                    _logger.LogInformation("[DeleteAvatarAsync] Eliminazione completata per UserId={UserId}", userId);
                }

                return ServiceResult<bool>.SuccessResult(true, "Avatar eliminato con successo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting base64 avatar");
                return ServiceResult<bool>.ErrorResult("Errore eliminazione avatar");
            }
        }

        public async Task<ServiceResult<string>> GetAvatarUrlAsync(Guid userId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FajrDbContext>();
            var user = await db.Users.FindAsync(userId);
            _logger.LogInformation("[GetAvatarUrlAsync] Recupero avatar per UserId={UserId}. HasAvatar={HasAvatar}", userId, !string.IsNullOrEmpty(user?.ProfilePicture));
            return ServiceResult<string>.SuccessResult(user?.ProfilePicture ?? string.Empty, "Avatar recuperato");
        }

        public bool IsValidImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("[IsValidImageFile] File nullo o vuoto");
                return false;
            }
            if (file.Length > _maxFileSize)
            {
                _logger.LogWarning("[IsValidImageFile] File troppo grande: {Size} bytes (max {Max})", file.Length, _maxFileSize);
                return false;
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var isAllowed = _allowedExtensions.Contains(ext);
            _logger.LogInformation("[IsValidImageFile] Estensione={Ext}, ContentType={ContentType}, Size={Size}, Valido={IsAllowed}", ext, file.ContentType, file.Length, isAllowed);
            return isAllowed;
        }

        public string GeneratePreSignedUrl(string key, int minutes = 15)
        {
            // Non più supportato con salvataggio Base64 inline
            return string.Empty;
        }
    }
}
