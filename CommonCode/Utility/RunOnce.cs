
using BFormDomain.Validation;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace BFormDomain.HelperClasses;

public static class RunOnce
{
    private static readonly ConcurrentDictionary<string, bool> _ran = new();
    private static readonly object _lock = new();
    private static readonly SemaphoreSlim _asyncLock = new(1,1);

    public static void ThisCode(Action action, [CallerFilePath] string file = "unk", [CallerLineNumber]  int ln = -1)
    {
        file.Requires().IsNotEqualTo("unk");
        ln.Requires().IsGreaterOrEqual(0);
        
        var name = $"RAN:{file}--{ln}";
        if (_ran.ContainsKey(name))
            return;

        lock(_lock)
        {
            if (_ran.ContainsKey(name))
                return;

            action();

            _ran[name] = true;
        }
    }

    public async static Task ThisAsyncCode(Func<Task> action, [CallerFilePath] string file = "unk", [CallerLineNumber] int ln = -1)
    {
        file.Requires().IsNotEqualTo("unk");
        ln.Requires().IsGreaterOrEqual(0);

        var name = $"RAN:{file}--{ln}";
        if (_ran.ContainsKey(name))
            return;

        await _asyncLock.WaitAsync();
        try
        {
            if (_ran.ContainsKey(name))
                return;

            await action();

            _ran[name] = true;
        } finally
        {
            _asyncLock.Release();
        }
    }

}
