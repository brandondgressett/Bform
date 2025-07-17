using BFormDomain.DataModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BFormDomain.CommonCode.Repository.Mongo;
using BFormDomain.Repository;
using BFormDomain.Mongo;

namespace BFormDomain.Repository.Mongo;

/// <summary>
/// Factory for creating MongoDB repository instances.
/// Works with both native MongoDB and Cosmos DB with MongoDB API.
/// </summary>
public class MongoRepositoryFactory : IRepositoryFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MongoDataEnvironment _dataEnvironment;
    private readonly IOptions<MongoRepositoryOptions> _options;
    private readonly ILoggerFactory _loggerFactory;

    public MongoRepositoryFactory(
        IServiceProvider serviceProvider,
        IOptions<MongoRepositoryOptions> options,
        ILoggerFactory loggerFactory)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _dataEnvironment = new MongoDataEnvironment(options);
        _loggerFactory = loggerFactory;
        
        // Log if we're using Cosmos DB
        if (_options.Value.MongoConnectionString.Contains("cosmos.azure.com"))
        {
            var logger = loggerFactory.CreateLogger<MongoRepositoryFactory>();
            logger.LogInformation("Using Cosmos DB with MongoDB API");
        }
    }

    /// <summary>
    /// Creates a repository instance for the specified type.
    /// Note: Since MongoRepository is abstract, this needs to be overridden
    /// to create concrete implementations for each entity type.
    /// </summary>
    public IRepository<T> CreateRepository<T>() where T : class, IDataModel
    {
        // This is a simplified implementation
        // In practice, you would:
        // 1. Use a registry of concrete repository types
        // 2. Use dependency injection to resolve the correct type
        // 3. Or use reflection to find the concrete implementation
        
        throw new NotImplementedException(
            $"No concrete repository implementation found for {typeof(T).Name}. " +
            "Register a concrete implementation that extends MongoRepository<T>.");
    }

    /// <summary>
    /// Gets the data environment for managing connections and transactions.
    /// </summary>
    public IDataEnvironment GetDataEnvironment()
    {
        return _dataEnvironment;
    }
}