using BFormDomain.CommonCode.Utility;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.MessageBus;
using BFormDomain.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BFormDomain.CommonCode.Logic.ConsolidateDigest;

/// <summary>
/// Listens for digestible items, assigns and appends them into
/// ongoing digests.
/// </summary>
public class ConsolidationService: IHostedService, IDisposable
{
    private IMessageListener? _qListener;
    private readonly IMessageBusSpecifier _busSpec;
    
    private readonly IConsolidationCore _core;
    private readonly ILogger<ConsolidationService> _logger;
    private readonly IApplicationAlert _alerts;
    private CancellationToken _ct;

    public ConsolidationService(KeyInject<string,IMessageListener>.ServiceResolver listener,
                                KeyInject<string,IMessageBusSpecifier>.ServiceResolver busSpec,
                                IConsolidationCore core,
                                ILogger<ConsolidationService> logger,
                                IApplicationAlert alerts)
    {
        _qListener = listener(MessageBusTopology.Distributed.EnumName());
        _busSpec = busSpec(MessageBusTopology.Distributed.EnumName());
        _core = core;
        _logger = logger;
        _alerts = alerts;
        
    }
    public void Dispose()
    {
        _qListener?.Dispose();
        _qListener = null!;
        GC.SuppressFinalize(this);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ct = cancellationToken;

        if (_qListener != null)
        {
            _busSpec
                .DeclareExchange(_core.ExchangeName, ExchangeTypes.Direct)
                .SpecifyExchange(_core.ExchangeName)
                .DeclareQueue(_core.QueueName, _core.QueueName);

            _logger.LogInformation($"Consolidation Service listening on {_core.ExchangeName}.{_core.QueueName}");

            _qListener.Initialize(_core.ExchangeName, _core.QueueName);
            _qListener.Listen(
                new KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>
                (typeof(ConsolidateDigestMessage), ProcessMessage));

        }

        return Task.CompletedTask;

    }

    private void ProcessMessage(object msg, CancellationToken ct, IMessageAcknowledge ack)
    {
        var item = msg as ConsolidateDigestMessage;
        
        if (!_ct.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            try
            {
                item.Guarantees("Could not unbox digestible item in consolidation service").IsNotNull();

                _logger.LogInformation($"Consolidation Service Adding to digest: {item!.DigestBodyJson}");
                AsyncHelper.RunSync(() => _core.ConsolidateAppendAsync(item!));

                ack.MessageAcknowledged();
            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General, LogLevel.Information, ex.TraceInformation(), 5);
                ack.MessageRejected();
            }
        }
        else
            ack.MessageRejected();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_qListener != null)
            _qListener.Paused = true;
        return Task.CompletedTask;
    }



}
