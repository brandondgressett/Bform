using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Utility
{
    /// <summary>
    /// Interface for cache services in BFormDomain
    /// </summary>
    public interface ICacheService
    {
        Task<bool> ClearAsync(string? pattern = null);
        Task<object> GetStatisticsAsync();
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
        Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
        Task<List<string>> GetKeysAsync(string pattern = "*", CancellationToken cancellationToken = default);
        Task<Dictionary<string, object>> GetMultipleAsync(List<string> keys, CancellationToken cancellationToken = default);
    }
}