using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using System.Collections.Concurrent;

using BFormDomain.Mongo;
using BFormDomain.Repository;
namespace BFormDomain.Mongo;

/// <summary>
/// Factory for creating optimized MongoDB clients with connection pooling and performance settings
/// </summary>
internal static class MongoClientFactory
{
    private static readonly ConcurrentDictionary<string, MongoClient> _clients = new();

    /// <summary>
    /// Creates or retrieves a cached MongoClient with optimized settings
    /// </summary>
    public static MongoClient CreateClient(string connectionString, MongoRepositoryOptions options)
    {
        // Create a cache key based on connection string and key settings
        var cacheKey = $"{connectionString}_{options.MaxConnectionPoolSize}_{options.ReadPreference}";

        return _clients.GetOrAdd(cacheKey, key =>
        {
            var mongoUrl = new MongoUrl(connectionString);
            var settings = MongoClientSettings.FromUrl(mongoUrl);

            // Connection pool settings
            settings.MaxConnectionPoolSize = options.MaxConnectionPoolSize;
            settings.MinConnectionPoolSize = options.MinConnectionPoolSize;
            settings.WaitQueueTimeout = TimeSpan.FromMilliseconds(options.WaitQueueTimeoutMs);
            settings.MaxConnectionIdleTime = TimeSpan.FromMilliseconds(options.ConnectionIdleTimeoutMs);
            settings.MaxConnectionLifeTime = TimeSpan.FromMilliseconds(options.ConnectionLifetimeMs);

            // Timeout settings
            settings.ConnectTimeout = TimeSpan.FromMilliseconds(options.CommandTimeoutMs);
            settings.SocketTimeout = TimeSpan.FromMilliseconds(options.SocketTimeoutMs);
            settings.ServerSelectionTimeout = TimeSpan.FromMilliseconds(options.CommandTimeoutMs);
            settings.HeartbeatInterval = TimeSpan.FromMilliseconds(options.HeartbeatIntervalMs);

            // SSL/TLS settings
            if (options.UseSsl)
            {
                settings.UseTls = true;
                settings.AllowInsecureTls = false; // Always use secure TLS in production
            }

            // Read preference
            settings.ReadPreference = ParseReadPreference(options.ReadPreference, options.MaxStalenessSeconds);

            // Write concern
            settings.WriteConcern = ParseWriteConcern(options.WriteConcern, options.WriteConcernTimeoutMs);

            // Read concern - use majority for better consistency
            settings.ReadConcern = ReadConcern.Majority;

            // Performance settings
            settings.RetryWrites = true;
            settings.RetryReads = true;

            // Server monitoring
            if (options.EnableServerMonitoring)
            {
                settings.HeartbeatInterval = TimeSpan.FromMilliseconds(options.HeartbeatIntervalMs);
            }

            // Connection mode - use direct connection for better performance in replica sets
            settings.DirectConnection = false;

            return new MongoClient(settings);
        });
    }

    /// <summary>
    /// Parses read preference string to MongoDB ReadPreference
    /// </summary>
    private static ReadPreference ParseReadPreference(string preference, int maxStalenessSeconds)
    {
        var maxStaleness = TimeSpan.FromSeconds(maxStalenessSeconds);

        return preference.ToLowerInvariant() switch
        {
            "primary" => ReadPreference.Primary,
            "primarypreferred" => ReadPreference.PrimaryPreferred,
            "secondary" => ReadPreference.Secondary.With(maxStaleness: maxStaleness),
            "secondarypreferred" => ReadPreference.SecondaryPreferred.With(maxStaleness: maxStaleness),
            "nearest" => ReadPreference.Nearest.With(maxStaleness: maxStaleness),
            _ => ReadPreference.Primary
        };
    }

    /// <summary>
    /// Parses write concern string to MongoDB WriteConcern
    /// </summary>
    private static WriteConcern ParseWriteConcern(string concern, int timeoutMs)
    {
        var timeout = TimeSpan.FromMilliseconds(timeoutMs);

        return concern.ToLowerInvariant() switch
        {
            "acknowledged" => WriteConcern.Acknowledged,
            "w1" => WriteConcern.W1,
            "w2" => WriteConcern.W2,
            "w3" => WriteConcern.W3,
            "majority" => WriteConcern.WMajority.With(wTimeout: timeout),
            "journaled" => WriteConcern.Acknowledged.With(journal: true, wTimeout: timeout),
            _ => WriteConcern.Acknowledged
        };
    }

    /// <summary>
    /// Clears the client cache (useful for testing or configuration changes)
    /// </summary>
    public static void ClearCache()
    {
        _clients.Clear();
    }
}