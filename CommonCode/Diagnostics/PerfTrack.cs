using BFormDomain.HelperClasses;
using BFormDomain.Validation;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace BFormDomain.Diagnostics;

public class PerfTrack
{
    private readonly Stopwatch _sw = new();
    private string _nm = "ERROR";
    private string _file = "unknown";
    private int _ln;

    public void ExplicitBegin(string nm, string file = "unknown", int ln = 0)
    {
        nm.Requires().IsNotNullOrEmpty();

        _nm = nm;
        _file = file;
        _ln = ln;
        _sw.Reset();
        _sw.Start();
    }

    public void Begin(string nm, [CallerFilePath] string file = "unknown", [CallerLineNumber]  int ln = 0)
    {
        ExplicitBegin(nm, file, ln);
    }

    public void End()
    {
        
        _sw.Stop();
        PerformanceMetric.IncRate(_nm, _file, _ln, _sw.ElapsedMilliseconds);
        
    }

    public static IDisposable Stopwatch(string nm, [CallerFilePath] string file = "unknown", [CallerLineNumber] int ln = 0)
    {
        var pt = new PerfTrack();
        pt.ExplicitBegin(nm, file, ln);
        return Disposable.Create(() => pt.End());
    }
}
