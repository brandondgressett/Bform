using BFormDomain.CommonCode.Utility;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.MessageBus;
using BFormDomain.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace BFormDomain.CommonCode.Logic.ConsolidateDigest;

/// <summary>
/// Distributes digests to consumers, then grooms them out of
/// the persistent storage.
/// </summary>
public class DigestDistributionService : IHostedService, IDisposable
{

    private readonly IConsolidationCore _core;
    private Timer? _timer = null!;
    private readonly IApplicationAlert _alerts;
    private readonly IServiceProvider _serviceProvider;
    private readonly DigestDistributionOptions _config;
    private CancellationToken _ct;
    private readonly ILogger<DigestDistributionService> _log;

    /// <summary>
    /// _pubFactory is an injected instance that uses exchange name passed into initialize function to publish items to the message bus specified.
    /// </summary>
    private readonly KeyInject<string, IMessagePublisher>.ServiceResolver _pubFactory;

    public DigestDistributionService(
        ILogger<DigestDistributionService> log,
        IApplicationAlert alerts, IConsolidationCore core, IServiceProvider sp, 
        IOptions<DigestDistributionOptions> cfg, KeyInject<string, IMessagePublisher>.ServiceResolver pubFactory) =>
        (_log, _alerts, _core, _serviceProvider, _config, _pubFactory) = (log,alerts, core, sp, cfg.Value,pubFactory);

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _ct = stoppingToken;
        _timer = new Timer(DoWork, null, TimeSpan.FromMinutes(_config.DistributionHeartbeatMinutes), TimeSpan.FromMinutes(_config.DistributionHeartbeatMinutes));
        return Task.CompletedTask;
    }

    private void DoWork(object? _)
    {
        try
        {
            var workItems = AsyncHelper.RunSync(()=>_core.GetCompletedDigestsAsync());
            foreach (var digest in workItems)
            {
                var forwardPub = _pubFactory(MessageBusTopology.Distributed.EnumName());
                
                forwardPub!.Initialize(digest.ForwardToExchange);
                var completeDigest = digest.CurrentDigest;
                string route = $"dig_rcv_{digest.ForwardToRoute}";
                forwardPub.Send(completeDigest, route);
#if DEBUG
                _log.LogInformation("sending digest:{json}", JsonConvert.SerializeObject(completeDigest, Formatting.Indented));
#endif
            }

            AsyncHelper.RunSync(()=>_core.GroomCompletedDigests(workItems));
        } catch(Exception ex)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.System, LogLevel.Information, ex.TraceInformation(),
                10);
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
