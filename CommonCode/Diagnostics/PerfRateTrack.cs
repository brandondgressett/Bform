using System.Collections.Concurrent;

namespace BFormDomain.Diagnostics;

public static partial class PerformanceMetric
{
    internal class PerfRateTrack
    {
        public string Name { get; set; } = "unknown";
        public DateTime Starting { get; set; }
        public int Count { get; set; }
        public double MaxMs { get; set; }
        public double MinMs { get; set; }
        public string File { get; set; } = "unknown";
        public int Line { get; set; }
        public string MachineName { get; set; } = "unknown";

        
        private readonly ConcurrentBag<double> _recordings = new(); 
        public void RecordReading(double ms)
        {
            _recordings.Add(ms);
        }

        public double Median
        {
            get 
            { 
                if (!_recordings.Any()) return 0.0;
                return _recordings.OrderBy(x => x).Skip(_recordings.Count / 2).First();
            }
        }

        public  double Average
        {
            get 
            { 
                if (!_recordings.Any()) return 0.0;
                return _recordings.Average();
            }
        }

        public double Sum
        {
            get
            {
                if (!_recordings.Any()) return 0.0;
                return _recordings.Sum();
            }
        }
    }
}
