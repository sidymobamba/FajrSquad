using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FajrSquad.Infrastructure.Services
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
        Task RemoveAsync(string key);
        Task RemoveByPatternAsync(string pattern);
    }

    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<MemoryCacheService> _logger;

        public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                _cache.TryGetValue(key, out T? value);
                return Task.FromResult(value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache key {Key}", key);
                return Task.FromResult<T?>(null);
            }
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            try
            {
                var options = new MemoryCacheEntryOptions();
                if (expiration.HasValue)
                    options.SetAbsoluteExpiration(expiration.Value);
                else
                    options.SetAbsoluteExpiration(TimeSpan.FromMinutes(30)); // Default 30 minutes

                _cache.Set(key, value, options);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache key {Key}", key);
                return Task.CompletedTask;
            }
        }

        public Task RemoveAsync(string key)
        {
            try
            {
                _cache.Remove(key);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache key {Key}", key);
                return Task.CompletedTask;
            }
        }

        public Task RemoveByPatternAsync(string pattern)
        {
            // Memory cache doesn't support pattern removal easily
            // This would require a more sophisticated implementation
            _logger.LogWarning("Pattern removal not implemented for MemoryCache");
            return Task.CompletedTask;
        }
    }
}