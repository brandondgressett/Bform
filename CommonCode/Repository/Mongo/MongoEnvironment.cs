using MongoDB.Driver;
using BFormDomain.Repository;

namespace BFormDomain.Mongo;

internal static class MongoEnvironment
{
    /// <summary>
    /// Creates a MongoClient using the optimized factory with connection pooling
    /// </summary>
    public static MongoClient MakeClient(string connStr, MongoRepositoryOptions options)
    {
        return MongoClientFactory.CreateClient(connStr, options);
    }

    /// <summary>
    /// Legacy method for backward compatibility - uses default options
    /// </summary>
    public static MongoClient MakeClient(string connStr)
    {
        var defaultOptions = new MongoRepositoryOptions
        {
            MongoConnectionString = connStr
        };
        return MongoClientFactory.CreateClient(connStr, defaultOptions);
    }
}
