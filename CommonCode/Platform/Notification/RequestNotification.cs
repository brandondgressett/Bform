using BFormDomain.CommonCode.Utility;
using BFormDomain.HelperClasses;
using BFormDomain.MessageBus;
using BFormDomain.Validation;

namespace BFormDomain.CommonCode.Notification;

/// <summary>
/// RequestNotification allows client to send notifications of all types by request
///     -References:
///         >SwitchingApplicationAlert.cs
///         >RuleActionRequestNotification.cs
///         >ReportLogic.cs
///     -Functions:
///         >Initialize
///         >Request
/// </summary>
public class RequestNotification
{
    private readonly IMessageBusSpecifier _busSpec;
    private readonly IMessagePublisher _pub;

    private readonly string _exchangeName;
    private readonly string _routeName;
    private bool _initialized = false;
    private readonly object _door = new();

    public RequestNotification(
        IMessageBusSpecifier busSpec,
        KeyInject<string, IMessagePublisher>.ServiceResolver pub)
    {
        _busSpec = busSpec;
        _pub = pub(MessageBusTopology.Distributed.EnumName());

        _exchangeName = _exchangeName = NotificationService.ExchangeName;
        _routeName = NotificationService.RouteName;
    }


    public void Initialize()
    {
        lock (_door)
        {
            if (!_initialized)
            {
                _busSpec
                        .DeclareExchange(_exchangeName, ExchangeTypes.Direct)
                        .SpecifyExchange(_exchangeName)
                        .DeclareQueue(_routeName, _routeName);

                _pub.Initialize(_exchangeName);
                                
                _initialized = true;
            }
        }
    }

    public async Task Request(NotificationMessage message)
    {
        Initialize();

        message.Requires().IsNotNull();
        bool hasTarget =
            message.NotificationGroups.Any() ||
            message.NotificationGroup.HasValue ||
            message.NotificationContact.HasValue;
        hasTarget.Requires().IsTrue();

        bool channelSelected =
            null != message.SMSText ||
            null != message.EmailText ||
            null != message.EmailHtmlText ||
            null != message.CallText ||
            null != message.ToastText;
        channelSelected.Requires().IsTrue();

        await _pub.SendAsync(message, _routeName);
    }

}
