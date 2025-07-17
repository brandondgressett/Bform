namespace BFormDomain.HelperClasses;

public class AnonymousDisposable : IDisposable
{
    private readonly Action _onDispose;

    public AnonymousDisposable(Action onDispose)
    {
        _onDispose = onDispose;
    }

    #region IDisposable Members

    public void Dispose()
    {
        _onDispose();
        GC.SuppressFinalize(this);
    }

    #endregion
}
