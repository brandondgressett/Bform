
namespace BFormDomain.Repository;

public interface ITransactionContext: IDisposable
{
    void Begin();
    
    Task CommitAsync(CancellationToken ct = default);
    void Commit(CancellationToken ct = default);
    Task AbortAsync(CancellationToken ct = default);
    void Abort(CancellationToken ct = default);

   
    
}
