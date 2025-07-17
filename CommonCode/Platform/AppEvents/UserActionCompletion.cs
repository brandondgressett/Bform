using BFormDomain.CommonCode.Notification;
using BFormDomain.CommonCode.Utility;
using BFormDomain.HelperClasses;
using BFormDomain.MessageBus;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.AppEvents;

/// <summary>
/// UserActionCompletion publishes an action completed message to message bus
///     -References:
///         >AppEventDistributer.cs
///     -Functions:
///         >SignalComplete
/// </summary>
/// <summary>
/// CAG RE
/// </summary>
public class UserActionCompletion
{
    /// <summary>
    /// CAG RE
    /// </summary>
    private readonly IMessageBusSpecifier _messageBusSpecifier;
    /// <summary>
    /// CAG RE
    /// </summary>
    private readonly IMessagePublisher _messagePublisher;

    /// <summary>
    /// CAG RE
    /// </summary>
    /// <param name="specifier"></param>
    /// <param name="publisher"></param>
    public UserActionCompletion(
        KeyInject<string,IMessageBusSpecifier>.ServiceResolver specifier,
        KeyInject<string,IMessagePublisher>.ServiceResolver publisher)
    {
        _messageBusSpecifier = specifier(MessageBusTopology.Distributed.EnumName());
        _messagePublisher = publisher(MessageBusTopology.Distributed.EnumName());
    }

    /// <summary>
    /// CAG RE
    /// </summary>
    /// <param name="user"></param>
    /// <param name="actionId"></param>
    /// <returns></returns>
    public async Task SignalComplete(Guid user, string actionId)
    {
        RunOnce.ThisCode(() =>
        {
            _messageBusSpecifier.DeclareExchange(AppEventMetadataMessages.AppEventMetadataExchange, ExchangeTypes.Fanout);
            _messagePublisher.Initialize(AppEventMetadataMessages.AppEventMetadataExchange.EnumName());
        });

        await _messagePublisher.SendAsync(new WebNotification
        {
            ForUserId = user.ToString(),
            Kind = WebNotificationKind.Action,
            Info = actionId
        }, AppEventMetadataMessages.AppEventLines);

    }

}
