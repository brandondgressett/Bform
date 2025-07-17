using System.Collections.Concurrent;

namespace BFormDomain.Diagnostics;

public static partial class PerformanceMetric
{

    private static readonly ConcurrentDictionary<string, PerfRateTrack> _rates = new();


    public static void IncRate(string name, string file, int ln, double ms)
    {
        
        if (!_rates.ContainsKey(name))
            _rates[name] = new PerfRateTrack {Name = name, Count = 0, Starting = DateTime.UtcNow, MaxMs = 0.0, File = file, Line = ln};

        var current = _rates[name];
        current.MachineName = Environment.MachineName;
        current.Count +=1;
        if (ms > current.MaxMs)
            current.MaxMs = ms;
        if (current.MinMs == 0)
            current.MinMs = ms;
        if (current.MinMs > ms)
            current.MinMs = ms;

        current.RecordReading(ms);
    }


    internal static PerfRateTrack[] Clear()
    {
        var retval = _rates.Values.ToArray();
        _rates.Clear();
        return retval;
    }

    public static void Report()
    {
        var data = _rates.Values.ToArray();

 
        foreach (var it in data)
        {
           
            Console.WriteLine($"{it.Name}, count:{it.Count}, max:{it.MaxMs}, min:{it.MinMs}, med:{it.Median}, sum:{it.Sum}, mean:{it.Average}, origin:{it.File} {it.Line}");
            
        }
    }
}
