using BFormDomain.Mongo;
using BFormDomain.Repository;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Repository.Mongo;

public class MongoDataEnvironment : IDataEnvironment
{
    private readonly string _connection;
    private readonly int _pageSize;
    private readonly MongoRepositoryOptions _options;

    public MongoDataEnvironment(IOptions<MongoRepositoryOptions> options)
    {
        _options = options.Value;
        _connection = _options.MongoConnectionString;
        _pageSize = _options.DefaultPageSize;
    }

    protected async Task<IClientSessionHandle> OpenMongoTransactionAsync(CancellationToken ct = default)
    {
        var mongoConnectStr = _connection;
        MongoClient client = MongoEnvironment.MakeClient(mongoConnectStr, _options);
        var session = await client.StartSessionAsync(cancellationToken: ct);
        session.StartTransaction();
        
        return session;
    }

    protected IClientSessionHandle OpenMongoTransaction(CancellationToken ct = default)
    {
        var mongoConnectStr = _connection;
        MongoClient client = MongoEnvironment.MakeClient(mongoConnectStr, _options);
        var session = client.StartSession(cancellationToken: ct);
        session.StartTransaction();
        
        return session;
    }

    public async Task<ITransactionContext> OpenTransactionAsync(CancellationToken ct = default)
    {
        var session = await OpenMongoTransactionAsync(ct);
        return new MongoTransactionContext(session);
    }

    public ITransactionContext OpenTransaction(CancellationToken ct = default)
    {
        var session = OpenMongoTransaction(ct);
        return new MongoTransactionContext(session);
    }

    public int PageSize { get { return _pageSize; } }
}
