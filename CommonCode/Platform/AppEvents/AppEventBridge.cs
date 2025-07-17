using BFormDomain.CommonCode.Utility;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.MessageBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BFormDomain.CommonCode.Platform.AppEvents;

/// <summary>
/// Sets up each server (in a horizontal scaling scenario) to share the load
/// from the event pump.
/// Register as singleton.
/// </summary>
public class AppEventBridge: IHostedService, IDisposable
{
    private IMessageListener? _qListener;
    private readonly IMessageBusSpecifier _busSpec;
    private readonly ILogger<AppEventBridge> _logger;
    private readonly IApplicationAlert _alerts;
    private readonly AppEventDistributer _distributer;
   

    public AppEventBridge(        
        KeyInject<string, IMessageListener>.ServiceResolver listener,
        KeyInject<string, IMessageBusSpecifier>.ServiceResolver busSpec,
        AppEventDistributer distributer,
        ILogger<AppEventBridge> logger,
        IApplicationAlert alerts)
    {
        _qListener = listener(MessageBusTopology.Distributed.EnumName());
        _busSpec = busSpec(MessageBusTopology.Distributed.EnumName());
        
        _distributer = distributer;
        _logger = logger;
        _alerts = alerts;
    }

    public void Dispose()
    {
        _qListener?.Dispose();
        _qListener = null!;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
     

        if (_qListener is not null)
        {
            var qName = AppEventConstants.EventRoute;
            _busSpec
                .DeclareExchange(AppEventConstants.EventExchange, ExchangeTypes.Direct)
                .SpecifyExchange(AppEventConstants.EventExchange)
                .DeclareQueue(AppEventConstants.EventRoute, qName);

            _qListener.Initialize(AppEventConstants.EventExchange, qName);
            _qListener.Listen(new KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>
                                    (typeof(AppEvent), ProcessMessage));

        }

        return Task.CompletedTask;

    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="ct"></param>
    /// <param name="ack"></param>
#pragma warning disable CA1068 // CancellationToken parameters must come last
    private void ProcessMessage(object msg, CancellationToken ct, IMessageAcknowledge ack)
#pragma warning restore CA1068 // CancellationToken parameters must come last
    {
        var @event = (msg as AppEvent)!;
        try
        {
            AsyncHelper.RunSync(()=>_distributer.DistributeEvent(@event, ack));
        }catch (Exception ex)
        {
            ack.MessageAbandoned();
            _alerts.RaiseAlert(ApplicationAlertKind.General, LogLevel.Error, ex.TraceInformation());
        }
        
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_qListener != null)
            _qListener.Paused = true;
        return Task.CompletedTask;
    }



}
