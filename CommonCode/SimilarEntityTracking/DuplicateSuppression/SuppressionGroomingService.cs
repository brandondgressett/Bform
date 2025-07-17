using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BFormDomain.CommonCode.Logic.DuplicateSuppression;

/// <summary>
/// 
/// </summary>
public class SuppressionGroomingService : IHostedService, IDisposable
{
    /// <summary>
    /// 
    /// </summary>
    // private readonly SuppressionRepository _repo;
    private readonly IRepository<SuppressedItem> _repo;

    /// <summary>
    /// 
    /// </summary>
    private readonly ILogger<SuppressionGroomingService> _logger;

    /// <summary>
    /// 
    /// </summary>
    private Timer _timer = null!;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repo"></param>
    /// <param name="logger"></param>
    //public SuppressionGroomingService(SuppressionRepository repo, ILogger<SuppressionGroomingService> logger) =>
    //    (_repo, _logger) = (repo, logger);
    public SuppressionGroomingService(IRepository<SuppressedItem> repo, ILogger<SuppressionGroomingService> logger) =>
        (_repo, _logger) = (repo, logger);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    public Task StartAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(DoWork, null, TimeSpan.FromHours(4.0), TimeSpan.FromHours(4.0));
        return Task.CompletedTask;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="_"></param>
    private void DoWork(object? _)
    {
        try
        {
            var oldTime = DateTime.UtcNow + TimeSpan.FromDays(-1.0); // TODO: Make grooming duration configurable
            //_repo.DeleteFilter(si => si.SuppressionStartTime < oldTime);
        }
        catch (Exception ex)
        {
            _logger.LogError("Suppression Grooming Failed: " + ex.TraceInformation());
        }

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    public Task StopAsync(CancellationToken stoppingToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 
    /// </summary>
    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }

}




