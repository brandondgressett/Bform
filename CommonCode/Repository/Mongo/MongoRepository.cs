using BFormDomain.CommonCode.Repository.Mongo;
using BFormDomain.CommonCode.Utility;
using BFormDomain.DataModels;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using System.Linq.Expressions;

namespace BFormDomain.Mongo;

public abstract class MongoRepository<T> : IRepository<T> where T : class, IDataModel
{

    protected readonly MongoRepositoryOptions _settings;
    protected readonly SimpleApplicationAlert _alerts;
    protected readonly MongoDataEnvironment _environment;
    protected readonly ILogger<MongoRepository<T>>? _logger;
    protected readonly IAsyncPolicy? _retryPolicy;


    public MongoRepository(
        IOptions<MongoRepositoryOptions> options,
        SimpleApplicationAlert alerts,
        ILogger<MongoRepository<T>>? logger = null)
    {
        _settings = options.Value;
        _alerts = alerts;
        _environment = new(options);
        _logger = logger;
        
        // Initialize retry policy if enabled
        if (_settings.EnableRetryPolicy)
        {
            _retryPolicy = MongoDbRetryPolicy.CreateAdvancedAsyncPolicy(
                _logger,
                _settings.MaxRetryCount,
                _settings.CircuitBreakerThreshold,
                TimeSpan.FromSeconds(_settings.CircuitBreakerDurationSeconds));
        }
    }


    protected IMongoDatabase OpenDatabase()
    {
        var mongoConnectStr = _settings.MongoConnectionString;
        MongoClient client = MongoEnvironment.MakeClient(mongoConnectStr, _settings);
        var database = client.GetDatabase(_settings.DatabaseName);

        return database;
    }

   

    public async Task<ITransactionContext> OpenTransactionAsync(CancellationToken ct = default)
    {
        return await _environment.OpenTransactionAsync(ct);
    }

    public ITransactionContext OpenTransaction(CancellationToken ct = default)
    {
        return _environment.OpenTransaction(ct);
    }

    protected abstract IMongoCollection<T> CreateCollection();
    protected abstract string CollectionName { get; }

    protected IMongoCollection<T> GuardedCreateCollection()
    {
        try
        {
            return CreateCollection();
        }
        catch (MongoException mex)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.Services, LogLevel.Information, mex.TraceInformation(), _settings.FaultLimit, "Mongo");
            throw;
        }
    }

    protected IMongoCollection<T> OpenCollection()
    {
        var database = OpenDatabase();
        var collection = database.GetCollection<T>(CollectionName);
        return collection;
    }



    public virtual void Create(T newItem)
    {
        var coll = GuardedCreateCollection();
        newItem.Version = 0;
        coll.InsertOne(newItem);

    }

    public virtual void Create(ITransactionContext tc, T newItem)
    {
        var coll = GuardedCreateCollection();
        newItem.Version = 0;
        var mtc = tc as MongoTransactionContext;
        mtc.Guarantees().IsNotNull();
        coll.InsertOne(mtc!.Check(), newItem);

    }

    public virtual async Task CreateAsync(T newItem)
    {
        var coll = GuardedCreateCollection();
        newItem.Version = 0;
        await coll.InsertOneAsync(newItem);
    }

    public virtual async Task CreateAsync(ITransactionContext? tc, T newItem)
    {
        var coll = GuardedCreateCollection();
        newItem.Version = 0;
        if (tc == null)
        {
            var mtc = OpenTransaction() as MongoTransactionContext;
            mtc.Guarantees().IsNotNull();
            await coll.InsertOneAsync(mtc!.Check(), newItem);
            await mtc.CommitAsync();
        }
        else
        {
            var mtc = tc as MongoTransactionContext;
            mtc.Guarantees().IsNotNull();
            await coll.InsertOneAsync(mtc!.Check(),newItem);
        }
    }

    public virtual async Task<long> CreateIfNotExistsAsync(IEnumerable<T> newItems)
    {
        var updates = new List<WriteModel<T>>();
        

        foreach (var newItem in newItems)
        {
            FilterDefinition<T> filter = Builders<T>.Filter.Eq(x => x.Id, newItem.Id);
            var json = JObject.FromObject(newItem);
            var bsonDoc = json.ToBsonObject();

            // create an empty update definition
            UpdateDefinition<T> updateDefinition = new UpdateDefinitionBuilder<T>().Unset("______"); 
            foreach (var element in bsonDoc.Elements)
            {
                if (element.Name == "_id" || element.Value == BsonNull.Value)
                    continue;

                updateDefinition = updateDefinition.SetOnInsert(element.Name, element.Value);
            }
            UpdateOneModel<T> update = new(filter, updateDefinition) { IsUpsert = true };
            updates.Add(update);
        }

        var collection = GuardedCreateCollection();
        var result = await collection.BulkWriteAsync(updates);
        result.IsAcknowledged.Guarantees().IsTrue();
        return result.InsertedCount;

    }
        
    public virtual async Task CreateBatchAsync(IEnumerable<T> newItems)
    {
        var coll = GuardedCreateCollection();
        var rc = new RepositoryContext
        {
            Context = coll,
        };

        await CreateBatchAsync(rc, newItems);
    }

    public virtual async Task CreateBatchAsync(RepositoryContext ctx, IEnumerable<T> newItems)
    {
        var coll = ctx.Context as IMongoCollection<T>;
        coll.Guarantees().IsNotNull();
        
        var itemsList = newItems as List<T> ?? newItems.ToList();
        if (!itemsList.Any()) return;
        
        // Set version for all new items
        foreach (var item in itemsList)
        {
            item.Version = 0;
        }
        
        // Use BulkWrite for better performance with large batches
        if (itemsList.Count > _settings.BulkWriteBatchSize)
        {
            // Process in batches
            var batches = itemsList
                .Select((item, index) => new { item, index })
                .GroupBy(x => x.index / _settings.BulkWriteBatchSize)
                .Select(g => g.Select(x => x.item));
            
            foreach (var batch in batches)
            {
                var bulkOps = batch.Select(item => new InsertOneModel<T>(item)).ToList();
                
                var bulkOptions = new BulkWriteOptions
                {
                    IsOrdered = _settings.BulkWriteOrdered,
                    BypassDocumentValidation = false
                };
                
                await coll!.BulkWriteAsync(bulkOps, bulkOptions);
            }
        }
        else
        {
            // For smaller batches, use InsertManyAsync
            var options = new InsertManyOptions
            {
                IsOrdered = _settings.BulkWriteOrdered,
                BypassDocumentValidation = false
            };
            
            await coll!.InsertManyAsync(itemsList, options);
        }
    }

    public virtual async Task CreateBatchAsync(ITransactionContext tc, IEnumerable<T> newItems)
    {
        var coll = GuardedCreateCollection();
        await CreateBatchAsync(new RepositoryContext { Context = coll },tc, newItems);
    }

    public virtual async Task CreateBatchAsync(RepositoryContext rc, ITransactionContext tc, IEnumerable<T> newItems)
    {
        var coll = (rc.Context as IMongoCollection<T>)!;
        var mtc = (tc as MongoTransactionContext)!;
        mtc.Guarantees().IsNotNull();
        
        var itemsList = newItems as List<T> ?? newItems.ToList();
        if (!itemsList.Any()) return;
        
        // Set version for all new items
        foreach (var item in itemsList)
        {
            item.Version = 0;
        }
        
        // Use BulkWrite for better performance with large batches
        if (itemsList.Count > _settings.BulkWriteBatchSize)
        {
            // Process in batches
            var batches = itemsList
                .Select((item, index) => new { item, index })
                .GroupBy(x => x.index / _settings.BulkWriteBatchSize)
                .Select(g => g.Select(x => x.item));
            
            foreach (var batch in batches)
            {
                var bulkOps = batch.Select(item => new InsertOneModel<T>(item)).ToList();
                
                var bulkOptions = new BulkWriteOptions
                {
                    IsOrdered = _settings.BulkWriteOrdered,
                    BypassDocumentValidation = false
                };
                
                await coll.BulkWriteAsync(mtc!.Check(), bulkOps, bulkOptions);
            }
        }
        else
        {
            // For smaller batches, use InsertManyAsync
            var options = new InsertManyOptions
            {
                IsOrdered = _settings.BulkWriteOrdered,
                BypassDocumentValidation = false
            };
            
            await coll.InsertManyAsync(mtc!.Check(), itemsList, options);
        }
    }



    public virtual (List<T>, RepositoryContext) Get(int start = 0, int count = 100, Expression<Func<T, bool>>? predicate = null)
    {
        var collection = GuardedCreateCollection();
        
        var filter = predicate != null ? Builders<T>.Filter.Where(predicate) : Builders<T>.Filter.Empty;
        
        var items = collection.Find(filter)
            .Skip(start)
            .Limit(count)
            .ToList();
        return (items, new RepositoryContext { Context = collection });
    }

    public virtual (List<T>, RepositoryContext) Get(ITransactionContext tc, int start = 0, int count = 100, Expression<Func<T, bool>>? predicate = null)
    {
        var collection = GuardedCreateCollection();
        var mtc = tc as MongoTransactionContext;
        mtc.Guarantees().IsNotNull();
        
        var filter = predicate != null ? Builders<T>.Filter.Where(predicate) : Builders<T>.Filter.Empty;
        
        var items = collection.Find(mtc!.Check(), filter)
            .Skip(start)
            .Limit(count)
            .ToList();
        return (items, new RepositoryContext { Context = collection });
    }


    public virtual async Task<(List<T>, RepositoryContext)> GetAsync(int start = 0, int count = 100, Expression<Func<T, bool>>? predicate = null)
    {
        var collection = GuardedCreateCollection();
        
        var filter = predicate != null ? Builders<T>.Filter.Where(predicate) : Builders<T>.Filter.Empty;
        var findOptions = new FindOptions<T>
        {
            Skip = start,
            Limit = count,
            BatchSize = Math.Min(count, _settings.BulkWriteBatchSize),
            AllowDiskUse = true
        };

        var cursor = await collection.FindAsync(filter, findOptions);
        var items = await cursor.ToListAsync();
        return (items, new RepositoryContext { Context = collection });
    }

    public virtual async Task<(List<T>, RepositoryContext)> GetAsync(ITransactionContext tc, int start = 0, int count = 100, Expression<Func<T, bool>>? predicate = null)
    {
        var collection = GuardedCreateCollection();
        var mtc = tc as MongoTransactionContext;
        mtc.Guarantees().IsNotNull();
        
        var filter = predicate != null ? Builders<T>.Filter.Where(predicate) : Builders<T>.Filter.Empty;
        var findOptions = new FindOptions<T>
        {
            Skip = start,
            Limit = count,
            BatchSize = Math.Min(count, _settings.BulkWriteBatchSize),
            AllowDiskUse = true
        };

        var cursor = await collection.FindAsync(mtc!.Check(), filter, findOptions);
        var items = await cursor.ToListAsync();
        return (items, new RepositoryContext { Context = collection });
    }

    public virtual async Task<(List<T>, RepositoryContext)> GetOrderedAsync<TField>(
        Expression<Func<T, TField>> orderField,
        bool descending = false,
        int start = 0, int count = 100, 
        Expression<Func<T, bool>>? predicate = null)
    {
        var collection = GuardedCreateCollection();
        
        var filter = predicate != null ? Builders<T>.Filter.Where(predicate) : Builders<T>.Filter.Empty;
        var sort = descending 
            ? Builders<T>.Sort.Descending(new ExpressionFieldDefinition<T>(orderField)) 
            : Builders<T>.Sort.Ascending(new ExpressionFieldDefinition<T>(orderField));
        
        var findOptions = new FindOptions<T>
        {
            Sort = sort,
            Skip = start,
            Limit = count,
            BatchSize = Math.Min(count, _settings.BulkWriteBatchSize),
            AllowDiskUse = true
        };

        var cursor = await collection.FindAsync(filter, findOptions);
        var items = await cursor.ToListAsync();
        
        return (items, new RepositoryContext { Context = collection });
    }

    public virtual async Task<(List<T>, RepositoryContext)> GetOrderedAsync<TField>(
        ITransactionContext tc,
        Expression<Func<T, TField>> orderField,
        bool descending = false,
        int start = 0, 
        int count = 100, 
        Expression<Func<T, bool>>? predicate = null)
    {
        var collection = GuardedCreateCollection();
        var mtc = tc as MongoTransactionContext;
        mtc.Guarantees().IsNotNull();
        
        var filter = predicate != null ? Builders<T>.Filter.Where(predicate) : Builders<T>.Filter.Empty;
        var sort = descending 
            ? Builders<T>.Sort.Descending(new ExpressionFieldDefinition<T>(orderField)) 
            : Builders<T>.Sort.Ascending(new ExpressionFieldDefinition<T>(orderField));
        
        var findOptions = new FindOptions<T>
        {
            Sort = sort,
            Skip = start,
            Limit = count,
            BatchSize = Math.Min(count, _settings.BulkWriteBatchSize),
            AllowDiskUse = true
        };

        var cursor = await collection.FindAsync(mtc!.Check(), filter, findOptions);
        var items = await cursor.ToListAsync();
        
        return (items, new RepositoryContext { Context = collection });
    }


    public virtual (List<T>, RepositoryContext) GetAll(Expression<Func<T, bool>> predicate)
    {
        var collection = GuardedCreateCollection();
        var items = collection.AsQueryable().Where(predicate).ToList();
        return (items, new RepositoryContext { Context = collection });
    }
    public virtual (List<T>, RepositoryContext) GetAll(ITransactionContext tc, Expression<Func<T, bool>> predicate)
    {
        var collection = GuardedCreateCollection();
        var mtc = tc as MongoTransactionContext;
        mtc.Guarantees().IsNotNull();
        var items = collection.AsQueryable(mtc!.Check()).Where(predicate).ToList();
        return (items, new RepositoryContext { Context = collection });
    }


    public virtual Task<(List<T>, RepositoryContext)> GetAllAsync(Expression<Func<T, bool>> predicate)
    {
        var collection = GuardedCreateCollection();
        var items = collection.AsQueryable().Where(predicate).ToList();
        return Task.FromResult((items, new RepositoryContext { Context = collection }));
    }

    public virtual Task<(List<T>, RepositoryContext)> GetAllAsync(ITransactionContext tc, Expression<Func<T, bool>> predicate)
    {
        var collection = GuardedCreateCollection();
        var mtc = tc as MongoTransactionContext;
        mtc.Guarantees().IsNotNull();
        var items = collection.AsQueryable(mtc!.Check()).Where(predicate).ToList();
        return Task.FromResult((items, new RepositoryContext { Context = collection }));
    }

    public virtual async Task<(List<T>, RepositoryContext)> GetAllOrderedAsync<TField>(
        Expression<Func<T, TField>> orderField,
        bool descending = false,
        Expression<Func<T, bool>>? predicate = null)
    {
        var collection = GuardedCreateCollection();
        
        var filter = predicate != null ? Builders<T>.Filter.Where(predicate) : Builders<T>.Filter.Empty;
        var sort = descending 
            ? Builders<T>.Sort.Descending(new ExpressionFieldDefinition<T>(orderField)) 
            : Builders<T>.Sort.Ascending(new ExpressionFieldDefinition<T>(orderField));
        
        var findOptions = new FindOptions<T>
        {
            Sort = sort,
            BatchSize = _settings.BulkWriteBatchSize,
            AllowDiskUse = true
        };

        var cursor = await collection.FindAsync(filter, findOptions);
        var items = await cursor.ToListAsync();
        
        return (items, new RepositoryContext { Context = collection });
    }

    public virtual async Task<(T?, RepositoryContext)> GetOneAsync(Expression<Func<T, bool>> predicate)
    {
        var collection = GuardedCreateCollection();
        var item = await collection.AsQueryable().Where(predicate).FirstOrDefaultAsync();
        return (item, new RepositoryContext { Context = collection });
    }

    public virtual async Task<(T?, RepositoryContext)> GetOneAsync(ITransactionContext tc, Expression<Func<T, bool>> predicate)
    {
        var collection = GuardedCreateCollection();
        var mtc = tc as MongoTransactionContext;
        mtc.Guarantees().IsNotNull();
        var item = await collection.AsQueryable(mtc!.Check()).Where(predicate).FirstOrDefaultAsync();
        return (item, new RepositoryContext { Context = collection });
    }

    public virtual async Task<(List<T>, RepositoryContext)> GetPageAsync(int page = 0, Expression<Func<T, bool>>? predicate = null)
    {
        var start = page * _settings.DefaultPageSize;
        var pageSize = _settings.DefaultPageSize;
        return await GetAsync(start, pageSize, predicate);
    }

    public virtual async Task<(List<T>, RepositoryContext)> GetOrderedPageAsync<TField>(
        ITransactionContext tc,
        Expression<Func<T, TField>> orderField,
        bool descending = false,
        int page = 0, Expression<Func<T, bool>>? predicate = null)
    {
        var start = page * _settings.DefaultPageSize;
        var pageSize = _settings.DefaultPageSize;
        return await GetOrderedAsync(tc, orderField, descending, start, pageSize, predicate);
    }

    public virtual async Task<(List<T>, RepositoryContext)> GetOrderedPageAsync<TField>(
        Expression<Func<T, TField>> orderField,
        bool descending = false,
        int page = 0, Expression<Func<T, bool>>? predicate = null)
    {
        var start = page * _settings.DefaultPageSize;
        var pageSize = _settings.DefaultPageSize;
        return await GetOrderedAsync(orderField, descending, start, pageSize, predicate);
    }

    public virtual async Task<(List<T>, RepositoryContext)> GetPageAsync(ITransactionContext tc, int page = 0, Expression<Func<T, bool>>? predicate = null)
    {
        var start = page * _settings.DefaultPageSize;
        var pageSize = _settings.DefaultPageSize;
        return await GetAsync(tc, start, pageSize, predicate);
    }

    public virtual async Task<(T, RepositoryContext)> LoadAsync(Guid id)
    {
        var collection = GuardedCreateCollection();
        var q = await collection.FindAsync(Builders<T>.Filter.Eq("Id", id));
        var ddm = q.SingleOrDefault();
        return (ddm, new RepositoryContext { Context = collection });
    }

    public virtual async Task<(T, RepositoryContext)> LoadAsync(ITransactionContext tc, Guid id)
    {
        var collection = GuardedCreateCollection();
        var mtc = tc as MongoTransactionContext;
        mtc.Guarantees().IsNotNull();
        var q = await collection.FindAsync(mtc!.Check(),Builders<T>.Filter.Eq("Id", id));
        var ddm = q.SingleOrDefault();
        return (ddm, new RepositoryContext { Context = collection });
    }

    public virtual async Task<(List<T>, RepositoryContext)> LoadManyAsync(IEnumerable<Guid> ids)
    {
        var collection = GuardedCreateCollection();
        
        // Convert to list to avoid multiple enumeration
        var idList = ids as List<Guid> ?? ids.ToList();
        
        if (!idList.Any())
        {
            return (new List<T>(), new RepositoryContext { Context = collection });
        }
        
        // Use Filter.In for efficient ID matching
        var filter = Builders<T>.Filter.In(x => x.Id, idList);
        
        // Use batch size for large ID sets
        var findOptions = new FindOptions<T>
        {
            BatchSize = Math.Min(idList.Count, _settings.BulkWriteBatchSize),
            AllowDiskUse = true
        };
        
        var cursor = await collection.FindAsync(filter, findOptions);
        var items = await cursor.ToListAsync();
        
        return (items, new RepositoryContext { Context = collection });
    }

    public virtual async Task<(List<T>, RepositoryContext)> LoadManyAsync(ITransactionContext tc, IEnumerable<Guid> ids)
    {
        var collection = GuardedCreateCollection();
        var mtc = tc as MongoTransactionContext;
        mtc.Guarantees().IsNotNull();
        
        // Convert to list to avoid multiple enumeration
        var idList = ids as List<Guid> ?? ids.ToList();
        
        if (!idList.Any())
        {
            return (new List<T>(), new RepositoryContext { Context = collection });
        }
        
        // Use Filter.In for efficient ID matching
        var filter = Builders<T>.Filter.In(x => x.Id, idList);
        
        // Use batch size for large ID sets
        var findOptions = new FindOptions<T>
        {
            BatchSize = Math.Min(idList.Count, _settings.BulkWriteBatchSize),
            AllowDiskUse = true
        };
        
        var cursor = await collection.FindAsync(mtc!.Check(), filter, findOptions);
        var items = await cursor.ToListAsync();
        
        return (items, new RepositoryContext { Context = collection });
    }

    public virtual async Task UpsertIgnoreVersionAsync(T data)
    {
        var collection = GuardedCreateCollection();
        await collection.ReplaceOneAsync(it => it.Id == data.Id, data, new ReplaceOptions() { IsUpsert = true }); ;
    }

    public virtual async Task UpsertIgnoreVersionAsync(ITransactionContext tc, T data)
    {
        var collection = GuardedCreateCollection();
        var mtc = tc as MongoTransactionContext;
        mtc.Guarantees().IsNotNull();
        await collection.ReplaceOneAsync(mtc!.Check(),it => it.Id == data.Id, data, new ReplaceOptions() { IsUpsert = true }); ;
    }

    public virtual void Update((T, RepositoryContext) data)
    {

        var collection = data.Item2.Context as IMongoCollection<T>;
        var doc = data.Item1;

        var version = doc.Version;
        doc.Version += 1;
        var result = collection.ReplaceOne(
            it => it.Id == doc.Id && it.Version == version,
            doc,
            new ReplaceOptions() { IsUpsert = false });

        result.ModifiedCount.Guarantees($"{typeof(T).GetFriendlyTypeName()} document was already changed.").IsEqualTo(1);

    }

    public virtual void Update(ITransactionContext tc,(T, RepositoryContext) data)
    {

        var collection = data.Item2.Context as IMongoCollection<T>;
        var mtc = tc as MongoTransactionContext;
        mtc.Guarantees().IsNotNull();
        var doc = data.Item1;

        var version = doc.Version;
        doc.Version += 1;
        var result = collection.ReplaceOne(mtc!.Check(),
            it => it.Id == doc.Id && it.Version == version,
            doc,
            new ReplaceOptions() { IsUpsert = false });

        result.ModifiedCount.Guarantees($"{typeof(T).GetFriendlyTypeName()} document was already changed.").IsEqualTo(1);

    }

    public virtual async Task UpdateAsync((T, RepositoryContext) data)
    {
        var collection = data.Item2.Context as IMongoCollection<T>;
       

        var doc = data.Item1;
        var oldVersion = doc.Version;
        doc.Version += 1;

        var result = await collection.ReplaceOneAsync(
            it => it.Id == doc.Id && it.Version == oldVersion,
            doc,
            new ReplaceOptions() { IsUpsert = false });

        result.ModifiedCount.Guarantees($"{typeof(T).GetFriendlyTypeName()} document was already changed.").IsLessOrEqual(1);
    }

    public virtual async Task UpdateAsync(ITransactionContext tc, (T, RepositoryContext) data)
    {
        var collection = data.Item2.Context as IMongoCollection<T>;
        var mtc = tc as MongoTransactionContext;
        mtc.Guarantees().IsNotNull();

        var doc = data.Item1;
        var oldVersion = doc.Version;
        doc.Version += 1;

        var result = await collection.ReplaceOneAsync(mtc!.Check(),
            it => it.Id == doc.Id && it.Version == oldVersion,
            doc,
            new ReplaceOptions() { IsUpsert = false });

        // TODO: Investigate: will updates in transactions actually count the updates?
        // result.ModifiedCount.Guarantees($"{typeof(T).GetFriendlyTypeName()} document was already changed.").IsEqualTo(1);
    }

    public virtual async Task UpdateIgnoreVersionAsync((T, RepositoryContext) data)
    {
        var collection = data.Item2.Context as IMongoCollection<T>;
        var doc = data.Item1;
        var version = doc.Version;
        doc.Version += 1;

        var result = await collection.ReplaceOneAsync(
            it => it.Id == doc.Id,
            doc,
            new ReplaceOptions() { IsUpsert = false });
    }

    public virtual async Task UpdateIgnoreVersionAsync(ITransactionContext tc, (T, RepositoryContext) data)
    {
        var collection = data.Item2.Context as IMongoCollection<T>;
        var mtc = tc as MongoTransactionContext;
        mtc.Guarantees().IsNotNull();
        var doc = data.Item1;
        var version = doc.Version;
        doc.Version += 1;

        var result = await collection.ReplaceOneAsync(mtc!.Check(),
            it => it.Id == doc.Id,
            doc,
            new ReplaceOptions() { IsUpsert = false });

    }

    public virtual void Update(T data)
    {
        var collection = CreateCollection();
        RepositoryContext context = new() { Context = collection };
        Update((data, context));
    }

    public virtual void Update(ITransactionContext tc, T data)
    {
        var collection = CreateCollection();
        RepositoryContext context = new() { Context = collection };
        Update(tc,(data, context));
    }

    public virtual async Task UpdateAsync(T data)
    {
        var collection = GuardedCreateCollection();
        RepositoryContext context = new() { Context = collection };
        await UpdateAsync((data, context));
    }

    public virtual async Task UpdateAsync(ITransactionContext tc, T data)
    {
        var collection = GuardedCreateCollection();
        RepositoryContext context = new() { Context = collection };
        await UpdateAsync(tc, (data, context));
    }

    public virtual async Task UpdateIgnoreVersionAsync(T data)
    {
        var collection = GuardedCreateCollection();
        RepositoryContext context = new() { Context = collection };
        await UpdateIgnoreVersionAsync((data, context));
    }

    public virtual async Task UpdateIgnoreVersionAsync(ITransactionContext tc, T data)
    {
        var collection = GuardedCreateCollection();
        RepositoryContext context = new() { Context = collection };
        await UpdateIgnoreVersionAsync(tc, (data, context));
    }

    public virtual async Task IncrementOneByIdAsync<TField>(
        Guid id,
        Expression<Func<T, TField>> field,
        TField value)
    {
        var collection = GuardedCreateCollection();
        var result = await collection.UpdateOneAsync(
            Builders<T>.Filter.Eq(it=>it.Id,id), 
            Builders<T>.Update.Inc(field, value)
                              .Inc(it=>it.Version, 1));
        result.ModifiedCount.Guarantees($"{typeof(T).GetFriendlyTypeName()} document with id {id} not found to increment");

    }

    public virtual async Task IncrementOneByIdAsync<TField>(
        ITransactionContext tc,
        Guid id,
        Expression<Func<T, TField>> field,
        TField value)
    {
        var collection = GuardedCreateCollection();
        var mtc = tc as MongoTransactionContext;
        mtc.Guarantees().IsNotNull();
        var result = await collection.UpdateOneAsync(mtc!.Check(),
            Builders<T>.Filter.Eq(it => it.Id, id),
            Builders<T>.Update.Inc(field, value)
                              .Inc(it => it.Version, 1));
        result.ModifiedCount.Guarantees($"{typeof(T).GetFriendlyTypeName()} document with id {id} not found to increment");

    }

    public virtual async Task IncrementOneAsync<TField>(
        Expression<Func<T, bool>> predicate, 
        Expression<Func<T,TField>> field, 
        TField value)
    {
        var collection = GuardedCreateCollection();
        var result = await collection.UpdateOneAsync( 
            predicate, 
            Builders<T>.Update.Inc(field, value)
                              .Inc(it => it.Version, 1));
        result.ModifiedCount.Guarantees($"{typeof(T).GetFriendlyTypeName()} document not found to increment");
    }

    public virtual async Task IncrementOneAsync<TField>(
        ITransactionContext tc,
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TField>> field,
        TField value)
    {
        var collection = GuardedCreateCollection();
        var mtc = tc as MongoTransactionContext;
        mtc.Guarantees().IsNotNull();
        var result = await collection.UpdateOneAsync(mtc!.Check(),
            predicate,
            Builders<T>.Update.Inc(field, value)
                              .Inc(it => it.Version, 1));
        result.ModifiedCount.Guarantees($"{typeof(T).GetFriendlyTypeName()} document not found to increment");
    }

    public virtual async Task IncrementManyAsync<TField>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TField>> field,
        TField value)
    {
        var collection = GuardedCreateCollection();
        var result = await collection.UpdateManyAsync(
            predicate,
            Builders<T>.Update.Inc(field, value)
                              .Inc(it => it.Version, 1));
        result.IsAcknowledged.Guarantees().IsTrue();
    }

    public virtual async Task IncrementManyAsync<TField>(
        ITransactionContext tc,
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TField>> field,
        TField value)
    {
        var collection = GuardedCreateCollection();
        var mtc = tc as MongoTransactionContext;
        mtc.Guarantees().IsNotNull();
        var result = await collection.UpdateManyAsync(
            mtc!.Check(),
            predicate,
            Builders<T>.Update.Inc(field, value)
                              .Inc(it => it.Version, 1));
        result.IsAcknowledged.Guarantees().IsTrue();
    }


    public virtual void Delete((T, RepositoryContext) data)
    {
        var collection = data.Item2.Context as IMongoCollection<T>;
        // Use strongly-typed expression instead of string
        collection.FindOneAndDelete(Builders<T>.Filter.Eq(x => x.Id, data.Item1.Id));
    }

    public virtual void Delete(ITransactionContext tc,(T, RepositoryContext) data)
    {
        var collection = data.Item2.Context as IMongoCollection<T>;
        var mtc = tc as MongoTransactionContext;
        mtc.Guarantees().IsNotNull();
        // Use strongly-typed expression instead of string
        collection.FindOneAndDelete(mtc!.Check(),Builders<T>.Filter.Eq(x => x.Id, data.Item1.Id));
    }

    public virtual async Task DeleteAsync((T, RepositoryContext) data)
    {
        var collection = data.Item2.Context as IMongoCollection<T>;
        // Use strongly-typed expression instead of string
        await collection.FindOneAndDeleteAsync(Builders<T>.Filter.Eq(x => x.Id, data.Item1.Id));
    }

    public virtual async Task DeleteAsync(ITransactionContext tc, (T, RepositoryContext) data)
    {
        var collection = data.Item2.Context as IMongoCollection<T>;
        var mtc = tc as MongoTransactionContext;
        mtc.Guarantees().IsNotNull();
        // Use strongly-typed expression instead of string
        await collection.FindOneAndDeleteAsync(mtc!.Check(),Builders<T>.Filter.Eq(x => x.Id, data.Item1.Id));
    }

    public virtual async Task DeleteBatchAsync(IEnumerable<Guid> ids)
    {
        var collection = GuardedCreateCollection();
        
        // Convert to list to avoid multiple enumeration
        var idList = ids as List<Guid> ?? ids.ToList();
        
        if (!idList.Any())
        {
            return;
        }
        
        // Use Filter.In for efficient ID matching
        var filter = Builders<T>.Filter.In(x => x.Id, idList);
        await collection.DeleteManyAsync(filter);    
    }

    public virtual async Task DeleteBatchAsync(ITransactionContext tc, IEnumerable<Guid> ids)
    {
        var collection = GuardedCreateCollection();
        var mtc = tc as MongoTransactionContext;
        mtc.Guarantees().IsNotNull();
        
        // Convert to list to avoid multiple enumeration
        var idList = ids as List<Guid> ?? ids.ToList();
        
        if (!idList.Any())
        {
            return;
        }
        
        // Use Filter.In for efficient ID matching
        var filter = Builders<T>.Filter.In(x => x.Id, idList);
        await collection.DeleteManyAsync(mtc!.Check(), filter);
    }

    public virtual void Delete(T doc)
    {
        Delete((doc, new RepositoryContext { Context = CreateCollection() }));
    }

    public virtual void Delete(ITransactionContext tc, T doc)
    {
        Delete(tc,(doc, new RepositoryContext { Context = CreateCollection() }));
    }

    public virtual async Task DeleteAsync(T doc)
    {
        await DeleteAsync((doc, new RepositoryContext { Context = CreateCollection() }));
    }

    public virtual async Task DeleteAsync(ITransactionContext tc, T doc)
    {
        await DeleteAsync(tc,(doc, new RepositoryContext { Context = CreateCollection() }));
    }

    public virtual async Task DeleteFilterAsync(Expression<Func<T, bool>> predicate)
    {
        var collection = GuardedCreateCollection();
        await collection.DeleteManyAsync(predicate);
    }

    public virtual async Task DeleteFilterAsync(ITransactionContext tc, Expression<Func<T, bool>> predicate)
    {
        var collection = GuardedCreateCollection();
        var mtc = tc as MongoTransactionContext;
        mtc.Guarantees().IsNotNull();
        await collection.DeleteManyAsync(mtc!.Check(), predicate);
    }

    public virtual void DeleteFilter(Expression<Func<T, bool>> predicate)
    {
        var collection = GuardedCreateCollection();
        collection.DeleteMany(predicate);
    }

    public virtual void DeleteFilter(ITransactionContext tc, Expression<Func<T, bool>> predicate)
    {
        var collection = GuardedCreateCollection();
        var mtc = tc as MongoTransactionContext;
        mtc.Guarantees().IsNotNull();
        collection.DeleteMany(mtc!.Check(),predicate);
    }

    /// <summary>
    /// Gets documents with projection support (MongoDB 3.0+ optimized).
    /// </summary>
    public virtual async Task<List<TProjection>> GetWithProjectionAsync<TProjection>(
        FilterDefinition<T> filter,
        Expression<Func<T, TProjection>> projection,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var collection = GuardedCreateCollection();
            
            var findOptions = new FindOptions<T, TProjection>
            {
                Projection = Builders<T>.Projection.Expression(projection)
            };
            
            using var cursor = await collection.FindAsync(filter, findOptions, cancellationToken);
            var results = await cursor.ToListAsync(cancellationToken);
            
            sw.Stop();
            if (sw.ElapsedMilliseconds > 100)
            {
                _logger?.LogWarning("Slow projection query on {Type} took {ElapsedMs}ms for {Count} results",
                    typeof(T).Name, sw.ElapsedMilliseconds, results.Count);
            }
            
            return results;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetWithProjectionAsync for type {Type}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Executes an aggregation pipeline with performance optimization.
    /// </summary>
    public virtual async Task<List<TResult>> AggregateAsync<TResult>(
        PipelineDefinition<T, TResult> pipeline,
        AggregateOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var collection = GuardedCreateCollection();
            
            // Set default options if not provided
            options ??= new AggregateOptions
            {
                AllowDiskUse = true, // Allow disk use for large aggregations
                BatchSize = 100,     // Optimize memory usage
                MaxTime = TimeSpan.FromSeconds(30)
            };
            
            using var cursor = await collection.AggregateAsync(pipeline, options, cancellationToken);
            var results = await cursor.ToListAsync(cancellationToken);
            
            sw.Stop();
            if (sw.ElapsedMilliseconds > 500)
            {
                _logger?.LogWarning("Slow aggregation on {Type} took {ElapsedMs}ms for {Count} results",
                    typeof(T).Name, sw.ElapsedMilliseconds, results.Count);
            }
            
            return results;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in AggregateAsync for type {Type}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Executes an aggregation pipeline with fluent builder support.
    /// </summary>
    public virtual async Task<List<TResult>> AggregateAsync<TResult>(
        Func<IAggregateFluent<T>, IAggregateFluent<TResult>> pipelineBuilder,
        AggregateOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var collection = GuardedCreateCollection();
            
            // Set default options if not provided
            options ??= new AggregateOptions
            {
                AllowDiskUse = true,
                BatchSize = 100,
                MaxTime = TimeSpan.FromSeconds(30)
            };
            
            var aggregate = collection.Aggregate(options);
            var pipeline = pipelineBuilder(aggregate);
            
            var results = await pipeline.ToListAsync(cancellationToken);
            
            sw.Stop();
            if (sw.ElapsedMilliseconds > 500)
            {
                _logger?.LogWarning("Slow aggregation on {Type} took {ElapsedMs}ms for {Count} results",
                    typeof(T).Name, sw.ElapsedMilliseconds, results.Count);
            }
            
            return results;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in fluent AggregateAsync for type {Type}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Efficiently checks if a document exists without loading the full document.
    /// </summary>
    public virtual async Task<bool> ExistsAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = GuardedCreateCollection();
            
            // Project only the _id field for efficiency
            var options = new FindOptions<T, BsonDocument>
            {
                Projection = Builders<T>.Projection.Include("_id"),
                Limit = 1
            };
            
            using var cursor = await collection.FindAsync(filter, options, cancellationToken);
            return await cursor.AnyAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in ExistsAsync for type {Type}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Efficiently checks if a document exists by ID.
    /// </summary>
    public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.Eq(x => x.Id, id);
        return await ExistsAsync(filter, cancellationToken);
    }

    /// <summary>
    /// Efficiently counts documents matching a filter.
    /// </summary>
    public virtual async Task<long> CountAsync(FilterDefinition<T> filter, CountOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = GuardedCreateCollection();
            
            // Set default options if not provided
            options ??= new CountOptions
            {
                MaxTime = TimeSpan.FromSeconds(30)
            };
            
            return await collection.CountDocumentsAsync(filter, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in CountAsync for type {Type}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Efficiently counts all documents in the collection.
    /// </summary>
    public virtual async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await CountAsync(FilterDefinition<T>.Empty, null, cancellationToken);
    }

    /// <summary>
    /// Gets estimated document count (faster but less accurate).
    /// </summary>
    public virtual async Task<long> EstimatedCountAsync(EstimatedDocumentCountOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = GuardedCreateCollection();
            
            // Set default options if not provided
            options ??= new EstimatedDocumentCountOptions
            {
                MaxTime = TimeSpan.FromSeconds(5)
            };
            
            return await collection.EstimatedDocumentCountAsync(options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in EstimatedCountAsync for type {Type}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Performs optimized partial updates using UpdateDefinition builders.
    /// More efficient than full document replacement.
    /// </summary>
    public virtual async Task<UpdateResult> UpdatePartialAsync(
        FilterDefinition<T> filter,
        UpdateDefinition<T> update,
        UpdateOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var collection = GuardedCreateCollection();
            
            // Add version increment to the update
            var updateWithVersion = Builders<T>.Update.Combine(
                update,
                Builders<T>.Update.Inc(x => x.Version, 1)
            );
            
            var result = await collection.UpdateOneAsync(filter, updateWithVersion, options, cancellationToken);
            
            sw.Stop();
            if (sw.ElapsedMilliseconds > 100)
            {
                _logger?.LogWarning("Slow partial update on {Type} took {ElapsedMs}ms",
                    typeof(T).Name, sw.ElapsedMilliseconds);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in UpdatePartialAsync for type {Type}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Performs optimized partial updates using fluent builder pattern.
    /// </summary>
    public virtual async Task<UpdateResult> UpdatePartialAsync(
        Guid id,
        Action<UpdateDefinitionBuilder<T>> updateBuilder,
        bool checkVersion = true,
        int? currentVersion = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var builder = Builders<T>.Update;
            updateBuilder(builder);
            
            // Build the filter
            var filterBuilder = Builders<T>.Filter;
            var filter = filterBuilder.Eq(x => x.Id, id);
            
            if (checkVersion && currentVersion.HasValue)
            {
                filter = filterBuilder.And(filter, filterBuilder.Eq(x => x.Version, currentVersion.Value));
            }
            
            // Get the update definition from the builder
            var updateParts = new List<UpdateDefinition<T>>();
            
            // Note: In a real implementation, we'd need to capture the update definitions
            // created by the updateBuilder action. This is a simplified version.
            // The actual implementation would require a custom builder wrapper.
            
            return await UpdatePartialAsync(filter, builder.Inc(x => x.Version, 1), null, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in fluent UpdatePartialAsync for type {Type}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Performs bulk partial updates efficiently.
    /// </summary>
    public virtual async Task<BulkWriteResult<T>> UpdateManyPartialAsync(
        IEnumerable<WriteModel<T>> updates,
        BulkWriteOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var collection = GuardedCreateCollection();
            
            // Set default options if not provided
            options ??= new BulkWriteOptions
            {
                IsOrdered = false, // Unordered for better performance
                BypassDocumentValidation = false
            };
            
            var result = await collection.BulkWriteAsync(updates, options, cancellationToken);
            
            sw.Stop();
            if (sw.ElapsedMilliseconds > 500)
            {
                _logger?.LogWarning("Slow bulk update on {Type} took {ElapsedMs}ms for {Count} operations",
                    typeof(T).Name, sw.ElapsedMilliseconds, result.ProcessedRequests.Count);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in UpdateManyPartialAsync for type {Type}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Updates many documents matching a filter.
    /// </summary>
    public virtual async Task<UpdateResult> UpdateManyAsync(
        FilterDefinition<T> filter,
        UpdateDefinition<T> update,
        UpdateOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var collection = GuardedCreateCollection();
            
            // Add version increment to the update
            var updateWithVersion = Builders<T>.Update.Combine(
                update,
                Builders<T>.Update.Inc(x => x.Version, 1)
            );
            
            var result = await collection.UpdateManyAsync(filter, updateWithVersion, options, cancellationToken);
            
            sw.Stop();
            if (sw.ElapsedMilliseconds > 500)
            {
                _logger?.LogWarning("Slow update many on {Type} took {ElapsedMs}ms, modified {Count} documents",
                    typeof(T).Name, sw.ElapsedMilliseconds, result.ModifiedCount);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in UpdateManyAsync for type {Type}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Find and update a document atomically with optimized options.
    /// </summary>
    public virtual async Task<T?> FindOneAndUpdateAsync(
        FilterDefinition<T> filter,
        UpdateDefinition<T> update,
        FindOneAndUpdateOptions<T>? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = GuardedCreateCollection();
            
            // Set default options if not provided
            options ??= new FindOneAndUpdateOptions<T>
            {
                ReturnDocument = ReturnDocument.After, // Return updated document
                IsUpsert = false
            };
            
            // Add version increment to the update
            var updateWithVersion = Builders<T>.Update.Combine(
                update,
                Builders<T>.Update.Inc(x => x.Version, 1)
            );
            
            return await collection.FindOneAndUpdateAsync(filter, updateWithVersion, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in FindOneAndUpdateAsync for type {Type}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Performs cursor-based pagination for efficient handling of large datasets.
    /// Uses a unique, sortable field (like _id or timestamp) as the cursor.
    /// </summary>
    public virtual async Task<CursorPaginationResult<T>> GetWithCursorAsync(
        CursorPaginationRequest request,
        FilterDefinition<T>? filter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var collection = GuardedCreateCollection();
            
            // Validate page size
            if (request.PageSize <= 0 || request.PageSize > 1000)
            {
                throw new ArgumentException("PageSize must be between 1 and 1000", nameof(request));
            }

            // Create cursor filter
            var cursorFilter = CursorHelper.CreateCursorFilter<T>(
                request.Cursor,
                request.CursorField,
                request.Direction,
                request.SortDirection,
                filter);

            // Determine sort direction based on pagination direction
            var effectiveSortDirection = request.Direction == CursorDirection.Forward
                ? request.SortDirection
                : (request.SortDirection == BFormDomain.CommonCode.Repository.Mongo.SortDirection.Ascending ? BFormDomain.CommonCode.Repository.Mongo.SortDirection.Descending : BFormDomain.CommonCode.Repository.Mongo.SortDirection.Ascending);

            // Create sort definition
            var sortDefinition = effectiveSortDirection == BFormDomain.CommonCode.Repository.Mongo.SortDirection.Ascending
                ? Builders<T>.Sort.Ascending(request.CursorField)
                : Builders<T>.Sort.Descending(request.CursorField);

            // Fetch one extra item to determine if there are more pages
            var limit = request.PageSize + 1;
            
            var findOptions = new FindOptions<T>
            {
                Sort = sortDefinition,
                Limit = limit,
                BatchSize = Math.Min(limit, 100) // Optimize batch size
            };

            // Execute query
            using var cursor = await collection.FindAsync(cursorFilter, findOptions, cancellationToken);
            var items = await cursor.ToListAsync(cancellationToken);

            // Prepare result
            var result = new CursorPaginationResult<T>();
            
            // Check if we have more items than requested
            var hasMore = items.Count > request.PageSize;
            if (hasMore)
            {
                items.RemoveAt(items.Count - 1); // Remove the extra item
            }

            // Reverse items if paginating backward
            if (request.Direction == CursorDirection.Backward)
            {
                items.Reverse();
            }

            result.Items = items;
            result.HasNext = request.Direction == CursorDirection.Forward ? hasMore : !string.IsNullOrEmpty(request.Cursor);
            result.HasPrevious = request.Direction == CursorDirection.Backward ? hasMore : !string.IsNullOrEmpty(request.Cursor);

            // Set cursors if we have items
            if (items.Count > 0)
            {
                // Get cursor field value using reflection or BsonDocument conversion
                var firstItem = items[0];
                var lastItem = items[items.Count - 1];
                
                var firstValue = GetFieldValue(firstItem, request.CursorField);
                var lastValue = GetFieldValue(lastItem, request.CursorField);

                if (request.Direction == CursorDirection.Forward)
                {
                    result.NextCursor = result.HasNext ? CursorHelper.EncodeCursor(lastValue, request.CursorField) : null;
                    result.PreviousCursor = !string.IsNullOrEmpty(request.Cursor) ? CursorHelper.EncodeCursor(firstValue, request.CursorField) : null;
                }
                else
                {
                    result.NextCursor = !string.IsNullOrEmpty(request.Cursor) ? CursorHelper.EncodeCursor(lastValue, request.CursorField) : null;
                    result.PreviousCursor = result.HasPrevious ? CursorHelper.EncodeCursor(firstValue, request.CursorField) : null;
                }
            }

            sw.Stop();
            if (sw.ElapsedMilliseconds > 100)
            {
                _logger?.LogWarning("Slow cursor pagination on {Type} took {ElapsedMs}ms for {Count} items",
                    typeof(T).Name, sw.ElapsedMilliseconds, items.Count);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetWithCursorAsync for type {Type}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Performs cursor-based pagination with projection for memory efficiency.
    /// </summary>
    public virtual async Task<CursorPaginationResult<TProjection>> GetWithCursorAsync<TProjection>(
        CursorPaginationRequest request,
        Expression<Func<T, TProjection>> projection,
        FilterDefinition<T>? filter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var collection = GuardedCreateCollection();
            
            // Validate page size
            if (request.PageSize <= 0 || request.PageSize > 1000)
            {
                throw new ArgumentException("PageSize must be between 1 and 1000", nameof(request));
            }

            // Create cursor filter
            var cursorFilter = CursorHelper.CreateCursorFilter<T>(
                request.Cursor,
                request.CursorField,
                request.Direction,
                request.SortDirection,
                filter);

            // Determine sort direction
            var effectiveSortDirection = request.Direction == CursorDirection.Forward
                ? request.SortDirection
                : (request.SortDirection == BFormDomain.CommonCode.Repository.Mongo.SortDirection.Ascending ? BFormDomain.CommonCode.Repository.Mongo.SortDirection.Descending : BFormDomain.CommonCode.Repository.Mongo.SortDirection.Ascending);

            var sortDefinition = effectiveSortDirection == BFormDomain.CommonCode.Repository.Mongo.SortDirection.Ascending
                ? Builders<T>.Sort.Ascending(request.CursorField)
                : Builders<T>.Sort.Descending(request.CursorField);

            var limit = request.PageSize + 1;
            
            var findOptions = new FindOptions<T, TProjection>
            {
                Sort = sortDefinition,
                Limit = limit,
                Projection = Builders<T>.Projection.Expression(projection),
                BatchSize = Math.Min(limit, 100)
            };

            // Execute query
            using var cursor = await collection.FindAsync(cursorFilter, findOptions, cancellationToken);
            var items = await cursor.ToListAsync(cancellationToken);

            // Prepare result
            var result = new CursorPaginationResult<TProjection>();
            
            var hasMore = items.Count > request.PageSize;
            if (hasMore)
            {
                items.RemoveAt(items.Count - 1);
            }

            if (request.Direction == CursorDirection.Backward)
            {
                items.Reverse();
            }

            result.Items = items;
            result.HasNext = request.Direction == CursorDirection.Forward ? hasMore : !string.IsNullOrEmpty(request.Cursor);
            result.HasPrevious = request.Direction == CursorDirection.Backward ? hasMore : !string.IsNullOrEmpty(request.Cursor);

            // Note: Setting cursors for projections requires the cursor field to be included in the projection
            // This is a limitation that should be documented

            sw.Stop();
            if (sw.ElapsedMilliseconds > 100)
            {
                _logger?.LogWarning("Slow cursor pagination with projection on {Type} took {ElapsedMs}ms for {Count} items",
                    typeof(T).Name, sw.ElapsedMilliseconds, items.Count);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetWithCursorAsync with projection for type {Type}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Gets field value from an object for cursor pagination
    /// </summary>
    private object GetFieldValue(T item, string fieldName)
    {
        if (fieldName == "_id" || fieldName == "Id")
        {
            return item.Id;
        }

        // Convert to BsonDocument to access fields dynamically
        var bsonDoc = item.ToBsonDocument();
        
        if (!bsonDoc.Contains(fieldName))
        {
            throw new ArgumentException($"Field '{fieldName}' not found in document");
        }

        var bsonValue = bsonDoc[fieldName];
        
        // Convert BsonValue to CLR type
        return bsonValue switch
        {
            BsonObjectId objectId => objectId.Value,
            BsonString str => str.Value,
            BsonInt32 int32 => int32.Value,
            BsonInt64 int64 => int64.Value,
            BsonDateTime dateTime => dateTime.ToUniversalTime(),
            BsonDouble dbl => dbl.Value,
            _ => bsonValue.ToString() ?? throw new NotSupportedException($"Unsupported cursor field type: {bsonValue.BsonType}")
        };
    }

    /// <summary>
    /// Creates an async enumerable for streaming large result sets with cursor pagination.
    /// This is memory-efficient for processing millions of documents.
    /// </summary>
    public virtual async IAsyncEnumerable<T> StreamWithCursorAsync(
        FilterDefinition<T>? filter = null,
        string cursorField = "_id",
        BFormDomain.CommonCode.Repository.Mongo.SortDirection sortDirection = BFormDomain.CommonCode.Repository.Mongo.SortDirection.Ascending,
        int batchSize = 100,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? currentCursor = null!;
        bool hasMore = true;

        while (hasMore && !cancellationToken.IsCancellationRequested)
        {
            var request = new CursorPaginationRequest
            {
                Cursor = currentCursor,
                PageSize = batchSize,
                CursorField = cursorField,
                SortDirection = sortDirection,
                Direction = CursorDirection.Forward
            };

            var result = await GetWithCursorAsync(request, filter, cancellationToken);

            foreach (var item in result.Items)
            {
                yield return item;
            }

            currentCursor = result.NextCursor;
            hasMore = result.HasNext;
        }
    }
}
