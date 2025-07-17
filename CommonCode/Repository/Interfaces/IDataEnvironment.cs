namespace BFormDomain.Repository;

public interface IDataEnvironment
{
    Task<ITransactionContext> OpenTransactionAsync(CancellationToken ct);
    ITransactionContext OpenTransaction(CancellationToken ct);

    int PageSize { get; }
}
