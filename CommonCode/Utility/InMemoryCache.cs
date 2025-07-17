using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace BFormDomain.CommonCode.Utility
{
    /// <summary>
    /// In-memory implementation of ICacheService using Microsoft.Extensions.Caching.Memory
    /// </summary>
    public class InMemoryCache : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly HashSet<string> _trackedKeys = new();
        private readonly object _keyLock = new();

        public InMemoryCache(IMemoryCache cache)
        {
            _cache = cache;
        }

        public Task<bool> ClearAsync(string? pattern = null)
        {
            lock (_keyLock)
            {
                if (string.IsNullOrEmpty(pattern) || pattern == "*")
                {
                    foreach (var key in _trackedKeys.ToList())
                    {
                        _cache.Remove(key);
                    }
                    _trackedKeys.Clear();
                }
                else
                {
                    var keysToRemove = _trackedKeys
                        .Where(k => MatchesPattern(k, pattern))
                        .ToList();

                    foreach (var key in keysToRemove)
                    {
                        _cache.Remove(key);
                        _trackedKeys.Remove(key);
                    }
                }
            }
            return Task.FromResult(true);
        }

        public Task<object> GetStatisticsAsync()
        {
            var stats = new
            {
                Type = "InMemoryCache",
                KeyCount = _trackedKeys.Count,
                Keys = _trackedKeys.ToList()
            };
            return Task.FromResult<object>(stats);
        }

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue(key, out T? value))
            {
                return Task.FromResult(value);
            }
            return Task.FromResult<T?>(default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions();
            
            if (expiration.HasValue)
            {
                cacheEntryOptions.AbsoluteExpirationRelativeToNow = expiration.Value;
            }
            else
            {
                cacheEntryOptions.SlidingExpiration = TimeSpan.FromHours(1);
            }

            _cache.Set(key, value, cacheEntryOptions);
            
            lock (_keyLock)
            {
                _trackedKeys.Add(key);
            }
            
            return Task.CompletedTask;
        }

        public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _cache.Remove(key);
            
            lock (_keyLock)
            {
                _trackedKeys.Remove(key);
            }
            
            return Task.FromResult(true);
        }

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_cache.TryGetValue(key, out _));
        }

        public Task<List<string>> GetKeysAsync(string pattern = "*", CancellationToken cancellationToken = default)
        {
            lock (_keyLock)
            {
                if (pattern == "*")
                {
                    return Task.FromResult(_trackedKeys.ToList());
                }

                var matchingKeys = _trackedKeys
                    .Where(k => MatchesPattern(k, pattern))
                    .ToList();
                    
                return Task.FromResult(matchingKeys);
            }
        }

        public async Task<Dictionary<string, object>> GetMultipleAsync(List<string> keys, CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<string, object>();
            
            foreach (var key in keys)
            {
                if (_cache.TryGetValue(key, out object? value) && value != null)
                {
                    result[key] = value;
                }
            }
            
            return result;
        }

        private bool MatchesPattern(string key, string pattern)
        {
            if (pattern == "*") return true;
            
            // Simple wildcard matching
            pattern = pattern.Replace("*", ".*");
            return System.Text.RegularExpressions.Regex.IsMatch(key, $"^{pattern}$");
        }
    }
}