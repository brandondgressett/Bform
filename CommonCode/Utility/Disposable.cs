namespace BFormDomain.HelperClasses;

public static class Disposable
{
    public static IDisposable Create(Action a)
    {
        return new AnonymousDisposable(a);
    }

    public static IDisposable Enclose(object o)
    {
        if (o is IDisposable disposable)
            return disposable;

        return Create(() => { });
    }
}
