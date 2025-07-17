using BFormDomain.Mongo;

using BFormDomain.Repository;
namespace BFormDomain.Repository.CosmosDb;

/// <summary>
/// Configuration options for using Cosmos DB with MongoDB API.
/// Simply provides Cosmos DB specific connection details that work with MongoRepository.
/// </summary>
public class CosmosDbMongoOptions
{
    /// <summary>
    /// The Cosmos DB account name.
    /// </summary>
    public string AccountName { get; set; } = "";

    /// <summary>
    /// The Cosmos DB account key (primary or secondary).
    /// </summary>
    public string AccountKey { get; set; } = "";

    /// <summary>
    /// The database name.
    /// </summary>
    public string DatabaseName { get; set; } = "BFormDomain";

    /// <summary>
    /// Port for MongoDB API connection (default: 10255).
    /// </summary>
    public int Port { get; set; } = 10255;

    /// <summary>
    /// Whether to enable SSL (should always be true for Cosmos DB).
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Preferred regions for multi-region deployments.
    /// Example: ["East US", "West US 2"]
    /// </summary>
    public List<string> PreferredRegions { get; set; } = new();

    /// <summary>
    /// Application name for monitoring.
    /// </summary>
    public string ApplicationName { get; set; } = "BFormDomain";

    /// <summary>
    /// Gets the MongoDB connection string for Cosmos DB.
    /// </summary>
    public string GetConnectionString()
    {
        return CosmosDbMongoConfiguration.CreateConnectionString(
            AccountName,
            AccountKey,
            EnableSsl,
            Port);
    }

    /// <summary>
    /// Converts to MongoRepositoryOptions for use with MongoRepository.
    /// </summary>
    public MongoRepositoryOptions ToMongoRepositoryOptions()
    {
        return new MongoRepositoryOptions
        {
            MongoConnectionString = GetConnectionString(),
            DatabaseName = DatabaseName,
            UseSsl = EnableSsl,
            
            // Cosmos DB optimized settings
            MaxConnectionPoolSize = 100,
            MinConnectionPoolSize = 10,
            MaxRetryAttempts = 9,
            RetryDelayMs = 100,
            
            // Performance settings
            EnableRetryPolicy = true,
            EnablePerformanceCounters = true,
            
            // Read preference for multi-region
            ReadPreference = PreferredRegions.Any() ? "Nearest" : "Primary"
        };
    }
}