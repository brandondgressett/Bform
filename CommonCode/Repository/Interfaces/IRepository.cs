using BFormDomain.CommonCode.Repository.Mongo;
using BFormDomain.DataModels;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace BFormDomain.Repository;

public interface IRepository<T> where T : class, IDataModel
{
    Task<ITransactionContext> OpenTransactionAsync(CancellationToken ct = default);
    ITransactionContext OpenTransaction(CancellationToken ct = default);

    void Create(T newItem);
    Task CreateAsync(T newItem);
    Task CreateBatchAsync(IEnumerable<T> newItems);

    Task CreateBatchAsync(RepositoryContext ctx, IEnumerable<T> newItems);

    void Delete((T, RepositoryContext) data);
    void Delete(T doc);
    Task DeleteAsync((T, RepositoryContext) data);
    Task DeleteAsync(T doc);
    void DeleteFilter(Expression<Func<T, bool>> predicate);
    Task DeleteFilterAsync(Expression<Func<T, bool>> predicate);

    Task DeleteBatchAsync(IEnumerable<Guid> ids);

    (List<T>, RepositoryContext) Get(int start = 0, int count = 100, Expression<Func<T, bool>>? predicate = null);
    (List<T>, RepositoryContext) GetAll(Expression<Func<T, bool>> predicate);

    Task<(List<T>, RepositoryContext)> GetAllOrderedAsync<TField>(
        Expression<Func<T, TField>> orderField,
        bool descending = false,
        Expression<Func<T, bool>>? predicate = null);

    Task<(T?, RepositoryContext)> GetOneAsync(Expression<Func<T, bool>> predicate);

    Task<(List<T>, RepositoryContext)> GetAllAsync(Expression<Func<T, bool>> predicate);
    Task<(List<T>, RepositoryContext)> GetAsync(int start = 0, int count = 100, Expression<Func<T, bool>>? predicate = null);

    Task<(List<T>, RepositoryContext)> GetOrderedAsync<TField>(
        Expression<Func<T, TField>> orderField,
        bool descending = false,
        int start = 0, int count = 100,
        Expression<Func<T, bool>>? predicate = null);

    Task<(List<T>, RepositoryContext)> GetPageAsync(int page = 0, Expression<Func<T, bool>>? predicate = null);

    Task<(List<T>, RepositoryContext)> GetOrderedPageAsync<TField>(
      Expression<Func<T, TField>> orderField,
      bool descending = false,
      int page = 0, Expression<Func<T, bool>>? predicate = null);


    Task<(T, RepositoryContext)> LoadAsync(Guid id);
    Task<(List<T>, RepositoryContext)> LoadManyAsync(IEnumerable<Guid> ids);
    void Update((T, RepositoryContext) data);
    void Update(T data);
    Task UpdateAsync((T, RepositoryContext) data);
    Task UpdateAsync(T data);
    Task UpdateIgnoreVersionAsync((T, RepositoryContext) data);
    Task UpdateIgnoreVersionAsync(T data);
    Task UpsertIgnoreVersionAsync(T data);

    Task IncrementOneByIdAsync<TField>(Guid id,Expression<Func<T, TField>> field,TField value);

    Task IncrementOneAsync<TField>(Expression<Func<T, bool>> predicate, Expression<Func<T, TField>> field, TField value);

    Task IncrementManyAsync<TField>(Expression<Func<T, bool>> predicate, Expression<Func<T, TField>> field, TField value);



    void Create(ITransactionContext tc, T newItem);
    Task CreateAsync(ITransactionContext? tc, T newItem);

    Task CreateBatchAsync(ITransactionContext tc, IEnumerable<T> newItems);

    Task CreateBatchAsync(RepositoryContext rc, ITransactionContext tc, IEnumerable<T> newItems);
    void Delete(ITransactionContext tc, (T, RepositoryContext) data);
    void Delete(ITransactionContext tc, T doc);
    Task DeleteAsync(ITransactionContext tc, (T, RepositoryContext) data);
    Task DeleteAsync(ITransactionContext tc, T doc);
    void DeleteFilter(ITransactionContext tc, Expression<Func<T, bool>> predicate);
    Task DeleteFilterAsync(ITransactionContext tc, Expression<Func<T, bool>> predicate);
    Task DeleteBatchAsync(ITransactionContext tc, IEnumerable<Guid> ids);

    (List<T>, RepositoryContext) Get(ITransactionContext tc, int start = 0, int count = 100, Expression<Func<T, bool>>? predicate = null);
    (List<T>, RepositoryContext) GetAll(ITransactionContext tc, Expression<Func<T, bool>> predicate);
    Task<(T?, RepositoryContext)> GetOneAsync(ITransactionContext tc, Expression<Func<T, bool>> predicate);
    Task<(List<T>, RepositoryContext)> GetAllAsync(ITransactionContext tc, Expression<Func<T, bool>> predicate);
    Task<(List<T>, RepositoryContext)> GetAsync(ITransactionContext tc, int start = 0, int count = 100, Expression<Func<T, bool>>? predicate = null);

    Task<(List<T>, RepositoryContext)> GetOrderedAsync<TField>(
        ITransactionContext tc,
        Expression<Func<T, TField>> orderField,
        bool descending = false,
        int start = 0,
        int count = 100,
        Expression<Func<T, bool>>? predicate = null);

    Task<(List<T>, RepositoryContext)> GetPageAsync(ITransactionContext tc, int page = 0, Expression<Func<T, bool>>? predicate = null);

    Task<(List<T>, RepositoryContext)> GetOrderedPageAsync<TField>(
        ITransactionContext tc,
        Expression<Func<T, TField>> orderField,
        bool descending = false,
        int page = 0, Expression<Func<T, bool>>? predicate = null);


    Task<(T, RepositoryContext)> LoadAsync(ITransactionContext tc, Guid id);
    Task<(List<T>, RepositoryContext)> LoadManyAsync(ITransactionContext tc, IEnumerable<Guid> ids);
    void Update(ITransactionContext tc, (T, RepositoryContext) data);
    void Update(ITransactionContext tc, T data);
    Task UpdateAsync(ITransactionContext tc, (T, RepositoryContext) data);
    Task UpdateAsync(ITransactionContext tc, T data);
    Task UpdateIgnoreVersionAsync(ITransactionContext tc, (T, RepositoryContext) data);
    Task UpdateIgnoreVersionAsync(ITransactionContext tc, T data);
    Task UpsertIgnoreVersionAsync(ITransactionContext tc, T data);

    Task IncrementOneByIdAsync<TField>(ITransactionContext tc, Guid id, Expression<Func<T, TField>> field, TField value);

    Task IncrementOneAsync<TField>(ITransactionContext tc, Expression<Func<T, bool>> predicate, Expression<Func<T, TField>> field, TField value);

    Task IncrementManyAsync<TField>(ITransactionContext tc, Expression<Func<T, bool>> predicate, Expression<Func<T, TField>> field, TField value);

    #region Performance Optimized Methods

    /// <summary>
    /// Gets documents with projection support for memory efficiency.
    /// </summary>
    Task<List<TProjection>> GetWithProjectionAsync<TProjection>(
        FilterDefinition<T> filter,
        Expression<Func<T, TProjection>> projection,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an aggregation pipeline with performance optimization.
    /// </summary>
    Task<List<TResult>> AggregateAsync<TResult>(
        PipelineDefinition<T, TResult> pipeline,
        AggregateOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Efficiently checks if a document exists by ID.
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Efficiently checks if a document exists without loading the full document.
    /// </summary>
    Task<bool> ExistsAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Efficiently counts all documents in the collection.
    /// </summary>
    Task<long> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Efficiently counts documents matching a filter.
    /// </summary>
    Task<long> CountAsync(FilterDefinition<T> filter, CountOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs optimized partial updates using UpdateDefinition builders.
    /// </summary>
    Task<UpdateResult> UpdatePartialAsync(
        FilterDefinition<T> filter,
        UpdateDefinition<T> update,
        UpdateOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates many documents matching a filter.
    /// </summary>
    Task<UpdateResult> UpdateManyAsync(
        FilterDefinition<T> filter,
        UpdateDefinition<T> update,
        UpdateOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs cursor-based pagination for efficient handling of large datasets.
    /// </summary>
    Task<CursorPaginationResult<T>> GetWithCursorAsync(
        CursorPaginationRequest request,
        FilterDefinition<T>? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs cursor-based pagination with projection.
    /// </summary>
    Task<CursorPaginationResult<TProjection>> GetWithCursorAsync<TProjection>(
        CursorPaginationRequest request,
        Expression<Func<T, TProjection>> projection,
        FilterDefinition<T>? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an async enumerable for streaming large result sets.
    /// </summary>
    IAsyncEnumerable<T> StreamWithCursorAsync(
        FilterDefinition<T>? filter = null,
        string cursorField = "_id",
        BFormDomain.CommonCode.Repository.Mongo.SortDirection sortDirection = BFormDomain.CommonCode.Repository.Mongo.SortDirection.Ascending,
        int batchSize = 100,
        CancellationToken cancellationToken = default);

    #endregion
}
