using BFormDomain.Repository;
using BFormDomain.Validation;
using MongoDB.Driver;

namespace BFormDomain.CommonCode.Repository.Mongo;

public class MongoTransactionContext : ITransactionContext
{
    public MongoTransactionContext(IClientSessionHandle handle)
    {
        Handle = handle;
    }

    public IClientSessionHandle? Handle
    {
        get;
        set;
    }

    public IClientSessionHandle Check()
    {
        Handle!.Requires("using disposed transaction").IsNotNull();
        Handle!.IsInTransaction.Requires("No transaction started.").IsTrue();
        return Handle;
    }

    public void Begin()
    {
        Handle.Requires().IsNotNull();
        Handle!.StartTransaction();
    }
       

    public void Abort(CancellationToken ct = default)
    {
        Handle!.Requires("using disposed transaction").IsNotNull();
        Handle!.IsInTransaction.Requires("No transaction started.").IsTrue();
        Handle!.AbortTransaction();
    }

    public async Task AbortAsync(CancellationToken ct = default)
    {
        Handle!.Requires("using disposed transaction").IsNotNull();
        Handle!.IsInTransaction.Requires("No transaction started.").IsTrue();
        await Handle!.AbortTransactionAsync(ct);
    }

    public void Commit(CancellationToken ct = default)
    {
        Handle!.Requires("using disposed transaction").IsNotNull();
        Handle!.IsInTransaction.Requires("No transaction started.").IsTrue();
        Handle!.CommitTransaction(ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        Handle!.Requires("using disposed transaction").IsNotNull();
        Handle!.IsInTransaction.Requires("No transaction started.").IsTrue();
        await Handle!.CommitTransactionAsync(ct);
    }

    public void Dispose()
    {
            
        if(Handle is not null)
            Handle.Dispose();
        Handle = null!;
        GC.SuppressFinalize(this);

    }
}
