using BFormDomain.CommonCode.Utility;
using BFormDomain.HelperClasses;
using BFormDomain.MessageBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Notification;

/// <summary>
/// NotificationService implements StartAsync from IHostedService to call ProcessMessage on each instance of NotificationMessage listened for on the defined message bus 
///     -References:
///         >Service
///     -Functions:
///         >Dispose
///         >StartAsync
///         >ProcessMessage
///         >StopAsync
/// </summary>
public class NotificationService : IHostedService, IDisposable
{
    public const string ExchangeName = "user_notification";
    public const string RouteName = "q_user_notification";

    private IMessageListener? _qListener;
    private readonly IMessageBusSpecifier _busSpec;
    private readonly IRegulatedNotificationLogic _logic;
    private readonly ILogger<NotificationService> _logger;

    private readonly string _exchangeName;
    private readonly string _qName;

    private CancellationToken _ct;

    public NotificationService(KeyInject<string, IMessageListener>.ServiceResolver listener,
                               IMessageBusSpecifier busSpec,
                               IRegulatedNotificationLogic logic,
                               ILogger<NotificationService> logger)
    {
        (_qListener, _busSpec, _logic, _logger) = (listener(MessageBusTopology.Distributed.EnumName()), busSpec, logic, logger);
        _exchangeName = ExchangeName;
        _qName = RouteName;
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
                .DeclareExchange(_exchangeName, ExchangeTypes.Direct)
                .SpecifyExchange(_exchangeName)
                .DeclareQueue(_qName, _qName);

            _logger.LogInformation($"ApplicationUser Notification Service listening on {_exchangeName}.{_qName}");

            _qListener.Initialize(_exchangeName, _qName);
            _qListener.Listen(new KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>(typeof(NotificationMessage), ProcessMessage));

        }

        _logic.Initialize();

        return Task.CompletedTask;

    }

#pragma warning disable CA1068 // CancellationToken parameters must come last
    private void ProcessMessage(object msg, CancellationToken ct, IMessageAcknowledge ack)
#pragma warning restore CA1068 // CancellationToken parameters must come last
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            _ct.ThrowIfCancellationRequested();

            if (msg is NotificationMessage notification)
                AsyncHelper.RunSync(()=>_logic.Notify(notification));

        } catch (Exception ex)
        {
            _logger.LogWarning("{trace}", ex.TraceInformation());
        }

    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}