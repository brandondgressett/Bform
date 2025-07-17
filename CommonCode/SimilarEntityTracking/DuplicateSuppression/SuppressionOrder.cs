using BFormDomain.CommonCode.Utility;
using BFormDomain.HelperClasses;
using BFormDomain.MessageBus;
using BFormDomain.Validation;

namespace BFormDomain.CommonCode.Logic.DuplicateSuppression;

/// <summary>
/// SuppressionOrder<typeparamref name="T"/> sends instances of ICanShutUp 
/// to message bus for DuplicateSuppressionService to listen for and process. 
/// </summary>
/// <typeparam name="T">T can be any type that implements the ICanShutUp interface</typeparam>
public class SuppressionOrder<T>
    where T : class, ICanShutUp, new()
{

    /// <summary>
    /// _busSpec is an injected instance containing exchange/ route names for message bus.
    /// </summary>
    private readonly IMessageBusSpecifier _busSpec;

    /// <summary>
    /// _pub is an injected instance that uses exchange name to publish items to the message bus specified
    /// </summary>
    private readonly IMessagePublisher _pub;

    /// <summary>
    /// _exchangeName is used to set exchange for message bus specifier
    /// </summary>
    private readonly string _exchangeName;

    /// <summary>
    /// _routeName is used to set route for message bus specifier
    /// </summary>
    private readonly string _routeName;

    /// <summary>
    /// _defaultForwardExchange is the default exchange name for non-suppressed items
    /// </summary>
    private string _defaultForwardExchange = string.Empty;

    /// <summary>
    /// _defaultForwardRoute is the default route name for non-suppressed items
    /// </summary>
    private string _defaultForwardRoute = string.Empty;

    /// <summary>
    /// _defaultSuppressedExchange is the default exchange name for suppressed items
    /// </summary>
    private string? _defaultSuppressedExchange = null!;

    /// <summary>
    /// default route name for suppressed items
    /// </summary>
    private string? _defaultSuppressedRoute = null!;

    /// <summary>
    /// _initialized is checked before proceeding in Initialize() and is set to true within 
    /// </summary>
    private bool _initialized = false;

    /// <summary>
    /// _door is locked on before proceeding through Initialize() for thread safety
    /// </summary>
    private readonly object _door = new();

    /// <summary>
    /// DI Constructor. Register as transient.
    /// </summary>
    /// <param name="busSpec"></param>
    /// <param name="pub"></param>
    public SuppressionOrder(
        KeyInject<string,IMessageBusSpecifier>.ServiceResolver busSpec,
        KeyInject<string,IMessagePublisher>.ServiceResolver pub)
    {
        _busSpec = busSpec(MessageBusTopology.Distributed.EnumName());
        _pub = pub(MessageBusTopology.Distributed.EnumName());
        
        _exchangeName = _exchangeName = $"suppress_duplicates_{typeof(T).GetFriendlyTypeName()}";
        _routeName = $"{typeof(T).GetFriendlyTypeName()}";
    }

    /// <summary>
    /// -Sets default exchange and route names. 
    /// -Initializes message bus specifier and publisher.
    /// </summary>
    /// <param name="sendSuppressedToo">sendSuppressedToo determines if suppressed items should be handled. 
    /// -If true, suppressed items will be sent back on the suppressed items exchange. 
    /// -If false, then they will be discarded.</param>
    public void Initialize(bool sendSuppressedToo = false)
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
                               

                if(sendSuppressedToo)
                {
                    _defaultSuppressedExchange = $"rcv_suppressed_{typeof(T).GetFriendlyTypeName()}";
                    _defaultSuppressedRoute = $"q_suppressed_{typeof(T).GetFriendlyTypeName()}";
                }

                _defaultForwardExchange = $"rcv_passed_suppression_{typeof(T).GetFriendlyTypeName()}"; 
                _defaultForwardRoute = $"q_passed_suppression_{typeof(T).GetFriendlyTypeName()}";
                                
                _initialized = true;
            }
        }
    }

    /// <summary>
    /// Sends instance of SuppressDuplicatesMessage containing instance of ICanShutUp to message bus listened for in 
    /// DuplicateSuppressionService via the message bus publisher.
    /// </summary>
    /// <param name="item">Instance of ICanShutUp</param>

    public void MaybeSuppress(T item)
    {
        SuppressDuplicatesMessage message = Setup(item);

        _pub.Send(message, _routeName);
    }

    /// <summary>
    /// Sends instance of SuppressDuplicatesMessage asyncronously containing instance of 
    /// ICanShutUp to message bus listened for in DuplicateSuppressionService via the message bus publisher.
    /// </summary>
    /// <param name="item">Instance of ICanShutUp</param>
    /// 
    /// 
    /// 
    /// 
    /// <returns></returns>
    public async Task MaybeSuppressAsync(T item)
    {
        SuppressDuplicatesMessage message = Setup(item);
        await _pub.SendAsync(message, _routeName);
    }

    /// <summary>
    /// Returns instance of SuppressDuplicatesMessage containing 
    /// the instance of ICanShutUp and exchanges/routes.
    /// </summary>
    /// <param name="item">Instance of ICanShutUp</param>
    /// <returns>Returns the message containing the suppressed item and exchange/route 
    /// information to send to the message bus via the message bus publisher</returns>
    private SuppressDuplicatesMessage Setup(T item)
    {
        _initialized.Requires().IsTrue();
        item.Requires().IsNotNull();

        
        var forwardExchange = _defaultForwardExchange;
       
        var forwardRoute = _defaultForwardRoute;
       
        var suppressedExchange = _defaultSuppressedExchange;
       
        var suppressedRoute = _defaultSuppressedRoute;

        

        var message = new SuppressDuplicatesMessage
        {
            SuppressedItem = item,
            ForwardExchange = forwardExchange,
            ForwardQueue = forwardRoute,
            SuppressedExchange = suppressedExchange,
            SuppressedQueue = suppressedRoute
        };

        return message;
    }
}
