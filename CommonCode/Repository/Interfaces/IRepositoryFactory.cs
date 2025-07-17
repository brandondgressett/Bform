using BFormDomain.DataModels;

namespace BFormDomain.Repository;

/// <summary>
/// Factory interface for creating repository instances.
/// Allows for different repository implementations (MongoDB, CosmosDB, etc.)
/// </summary>
public interface IRepositoryFactory
{
    /// <summary>
    /// Creates a repository instance for the specified type.
    /// </summary>
    /// <typeparam name="T">The data model type</typeparam>
    /// <returns>A repository instance for type T</returns>
    IRepository<T> CreateRepository<T>() where T : class, IDataModel;

    /// <summary>
    /// Gets the data environment for managing connections and transactions.
    /// </summary>
    /// <returns>The data environment instance</returns>
    IDataEnvironment GetDataEnvironment();
}