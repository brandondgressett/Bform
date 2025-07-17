namespace BFormDomain.Repository.CosmosDb;

/// <summary>
/// Configuration helper for using Cosmos DB with MongoDB API.
/// This simply extends the MongoDB configuration with Cosmos DB specific settings.
/// </summary>
public static class CosmosDbMongoConfiguration
{
    /// <summary>
    /// Creates a MongoDB connection string for Cosmos DB.
    /// </summary>
    public static string CreateConnectionString(
        string accountName,
        string accountKey,
        bool enableSsl = true,
        int port = 10255)
    {
        return $"mongodb://{accountName}:{accountKey}@{accountName}.mongo.cosmos.azure.com:{port}/" +
               $"?ssl={enableSsl.ToString().ToLower()}" +
               "&replicaSet=globaldb" +
               "&retrywrites=false" +
               "&maxIdleTimeMS=120000" +
               $"&appName=@{accountName}@";
    }

    /// <summary>
    /// Gets the recommended MongoDB settings for Cosmos DB.
    /// </summary>
    public static Dictionary<string, object> GetRecommendedSettings()
    {
        return new Dictionary<string, object>
        {
            // Cosmos DB specific optimizations
            ["retryWrites"] = false, // Cosmos DB handles this internally
            ["w"] = "majority", // Write concern
            ["j"] = true, // Journal writes
            ["maxStalenessSeconds"] = 90, // For read preference
            ["maxIdleTimeMS"] = 120000, // Connection idle timeout
            
            // Performance settings
            ["maxPoolSize"] = 100,
            ["minPoolSize"] = 10,
            ["waitQueueTimeoutMS"] = 60000,
            ["serverSelectionTimeoutMS"] = 30000,
            
            // SSL/TLS
            ["ssl"] = true,
            ["sslValidate"] = true
        };
    }
}