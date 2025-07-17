using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace BFormDomain.CommonCode.Utility
{
    /// <summary>
    /// Redis implementation of ICacheService using StackExchange.Redis
    /// </summary>
    public class RedisCache : ICacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _database;
        private readonly int _databaseNumber;

        public RedisCache(IConnectionMultiplexer redis, int databaseNumber = 0)
        {
            _redis = redis;
            _databaseNumber = databaseNumber;
            _database = _redis.GetDatabase(databaseNumber);
        }

        public async Task<bool> ClearAsync(string? pattern = null)
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            
            if (string.IsNullOrEmpty(pattern) || pattern == "*")
            {
                await server.FlushDatabaseAsync(_databaseNumber);
                return true;
            }

            var keys = server.Keys(_databaseNumber, pattern: pattern).ToArray();
            if (keys.Any())
            {
                await _database.KeyDeleteAsync(keys);
            }
            
            return true;
        }

        public async Task<object> GetStatisticsAsync()
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var info = await server.InfoAsync();
            
            var stats = new
            {
                Type = "RedisCache",
                DatabaseNumber = _databaseNumber,
                KeyCount = await _database.ExecuteAsync("DBSIZE"),
                ServerInfo = info.ToDictionary(
                    group => group.Key,
                    group => group.ToDictionary(item => item.Key, item => item.Value)
                )
            };
            
            return stats;
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var value = await _database.StringGetAsync(key);
            
            if (!value.HasValue)
            {
                return default;
            }

            // Handle simple types
            if (typeof(T) == typeof(string))
            {
                return (T)(object)value.ToString();
            }
            
            if (typeof(T).IsPrimitive || typeof(T) == typeof(decimal))
            {
                return (T)Convert.ChangeType(value.ToString(), typeof(T));
            }

            // Handle complex types via JSON
            return JsonConvert.DeserializeObject<T>(value.ToString());
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            string stringValue;
            
            // Handle simple types
            if (value is string str)
            {
                stringValue = str;
            }
            else if (value?.GetType().IsPrimitive == true || value is decimal)
            {
                stringValue = value.ToString()!;
            }
            else
            {
                // Handle complex types via JSON
                stringValue = JsonConvert.SerializeObject(value);
            }

            await _database.StringSetAsync(key, stringValue, expiration);
        }

        public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            return await _database.KeyDeleteAsync(key);
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            return await _database.KeyExistsAsync(key);
        }

        public async Task<List<string>> GetKeysAsync(string pattern = "*", CancellationToken cancellationToken = default)
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = new List<string>();
            
            await foreach (var key in server.KeysAsync(_databaseNumber, pattern: pattern))
            {
                keys.Add(key.ToString());
            }
            
            return keys;
        }

        public async Task<Dictionary<string, object>> GetMultipleAsync(List<string> keys, CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<string, object>();
            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            var values = await _database.StringGetAsync(redisKeys);
            
            for (int i = 0; i < keys.Count; i++)
            {
                if (values[i].HasValue)
                {
                    try
                    {
                        // Try to deserialize as JSON first
                        var jsonValue = JsonConvert.DeserializeObject(values[i].ToString());
                        if (jsonValue != null)
                        {
                            result[keys[i]] = jsonValue;
                        }
                    }
                    catch
                    {
                        // If JSON deserialization fails, store as string
                        result[keys[i]] = values[i].ToString();
                    }
                }
            }
            
            return result;
        }
    }
}