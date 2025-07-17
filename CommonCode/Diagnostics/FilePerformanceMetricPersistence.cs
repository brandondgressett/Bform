using BFormDomain.HelperClasses;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BFormDomain.Diagnostics;

public class FilePerformanceMetricPersistence : IPerformanceMetricPersistence
{
   
    private readonly string _folder;
    private readonly int _retentionDays;
    private static readonly object _writeLock = new();
    private readonly ILogger<FilePerformanceMetricPersistence> _logger;
    private readonly IApplicationAlert _alerts;
    private readonly int _faultTolerance;
    

    public FilePerformanceMetricPersistence(
        ILogger<FilePerformanceMetricPersistence> logger,
        IOptions<FilePerformanceMetricPersistenceOptions> options,
        IApplicationAlert alerts)
    {
        _retentionDays = options.Value.RetentionDays;

        _retentionDays.Requires().IsGreaterOrEqual(1);
        
        _folder = Path.Combine(Environment.CurrentDirectory,options.Value.PerformanceReportPath);
        _faultTolerance = options.Value.FaultTolerance;

        _logger = logger;
        _alerts = alerts;
    }

    public void Record(
        DateTime start, DateTime end, 
        string name, 
        int count, 
        double rpm, double rps, 
        double maxMS, double minMS, double medMS, 
        double avgMS, double sumMS, 
        string file, int ln, string machine)
    {
        name.Requires().IsNotNullOrEmpty();
        count.Requires().IsGreaterOrEqual(0);
        file.Requires().IsNotNullOrEmpty();
        ln.Requires().IsGreaterOrEqual(0);
        machine.Requires().IsNotNullOrEmpty();

        var tc = new TemporalCollocator();
        tc.CollocateUtcNow();
        var thisDay = tc.TodayMidnight;
        var fn = $"{thisDay.Year}-{thisDay.Month}-{thisDay.Day}.csv";
        var filePath = Path.Combine(_folder, fn);
        lock (_writeLock)
        {
            try
            {
                if (!Directory.Exists(_folder))
                    Directory.CreateDirectory(_folder);
                File.AppendAllText(filePath, $"{start.ToShortDateString()} {start.ToShortTimeString()},{end.ToShortDateString()} {end.ToShortTimeString()},{name},{count},{rpm},{rps},{maxMS},{minMS},{medMS},{avgMS},{sumMS},{file},{ln},{machine}");
            } catch(Exception ex)
            {
                _logger.LogError(ex.TraceInformation());
                _alerts.RaiseAlert(ApplicationAlertKind.InputOutput, LogLevel.Information, ex.TraceInformation(),
                    _faultTolerance, nameof(FilePerformanceMetricPersistence));
            }
        }

        MaybeGroomOldFiles();

    }

    private void MaybeGroomOldFiles()
    {
        var cutTime = DateTime.Now.AddDays(-_retentionDays);
        var files = Directory.GetFiles(_folder);
        foreach (var f in files)
        {

            try
            {
                var fi = new FileInfo(f);
                if (fi.LastAccessTime < cutTime)
                    fi.Delete();
            }
            catch(Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.InputOutput, LogLevel.Information, ex.TraceInformation(),
                    _faultTolerance * 10);
            }
        }
    }


}
