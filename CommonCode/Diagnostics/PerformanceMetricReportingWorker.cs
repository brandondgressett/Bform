using BFormDomain.HelperClasses;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BFormDomain.Diagnostics;

public class PerformanceMetricReportingWorker: IHostedService, IDisposable
{
    private readonly IPerformanceMetricPersistence _mp;
    
    private Timer _timer = null!;

    private readonly IApplicationAlert _alerts;

    public PerformanceMetricReportingWorker(
        IApplicationAlert alerts, 
        IPerformanceMetricPersistence perfPersistence) =>
     (_alerts,_mp) = (alerts, perfPersistence);


    public Task StartAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(DoWork, null, TimeSpan.FromHours(1.0), TimeSpan.FromHours(1.0));
        return Task.CompletedTask;
    }

    private void DoWork(object? state)
    {
        var data = PerformanceMetric.Clear();
        var now = DateTime.UtcNow;
        foreach (var it in data)
        {
            try
            {
                var diff = now - it.Starting;
                var minutes = Math.Abs(diff.TotalMinutes);
                var seconds = Math.Abs(diff.TotalSeconds);
                var count = (double)it.Count;
                var rpm = count / minutes;
                var rps = count / seconds;
                _mp.Record(it.Starting, now, it.Name, it.Count, rpm, rps, it.MaxMs, it.MinMs, it.Median, it.Average, it.Sum, it.File, it.Line, it.MachineName);
            }
            catch (Exception ex)
            {
               
                _alerts.RaiseAlert(ApplicationAlertKind.System, LogLevel.Information, ex.TraceInformation(),
                    40);
            }
        }
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }

    
}
