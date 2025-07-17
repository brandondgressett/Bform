using BFormDomain.CommonCode.ApplicationTopology;
using BFormDomain.CommonCode.Platform.Tenancy;
using BFormDomain.CommonCode.Utility;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.MessageBus;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BFormDomain.CommonCode.Platform.AppEvents;

/// <summary>
/// CAG RE
/// Register as singleton.
/// </summary>
/// 
/// <summary>
/// AppEventPump gets events from mongo database through transactions so that 
/// failed actions wont continue to process to publish events to the specified message bus
///     -References:
///         >Background service
///     -Functions:
///         >MaybeInitialize
///         >ExecuteAsync
/// </summary>
public class AppEventPump : BackgroundService
{
    private readonly IRepository<AppEvent> _events;
    private readonly IMessagePublisher _publisher;
    private readonly IApplicationAlert _alert;
    private readonly IMessageBusSpecifier _messageBusSpecifier;
    private readonly ApplicationTopologyCatalog _topo;
    private readonly ITenantContext _tenantContext;
    private readonly MultiTenancyOptions _multiTenancyOptions;
    private readonly ILogger<AppEventPump> _logger;
    private readonly int _reenqueueTimeoutSeconds;
    private readonly int _retryCutOff;
    private readonly int _tooAgedMinutes;
    private bool _initialized = false;
    private readonly object _lock = new();

    public AppEventPump(
        IRepository<AppEvent> events,
        KeyInject<string,IMessageBusSpecifier>.ServiceResolver mbspec,
        KeyInject<string,IMessagePublisher>.ServiceResolver publisher,
        ApplicationTopologyCatalog topology,
        IApplicationAlert alert,
        ITenantContext tenantContext,
        IOptions<MultiTenancyOptions> multiTenancyOptions,
        ILogger<AppEventPump> logger,
        IOptions<AppEventPumpOptions> options
        ): base()
    {
        _events = events;
        _publisher = publisher(MessageBusTopology.Distributed.EnumName());
        _messageBusSpecifier = mbspec(MessageBusTopology.Distributed.EnumName());
        _topo = topology;
        _alert = alert;
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _multiTenancyOptions = multiTenancyOptions?.Value ?? throw new ArgumentNullException(nameof(multiTenancyOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var optionsVal = options.Value;
        _reenqueueTimeoutSeconds = optionsVal.ReEnqueueTimeoutSeconds;
        _retryCutOff = optionsVal.RetryCutOff;
        _tooAgedMinutes = optionsVal.TooAgedMinutes;
    }

    private void MaybeInitialize()
    {
        if (_initialized)
            return;
        lock (_lock)
        {
            if (!_initialized)
            {
                // send to a direct exchange to spread data between multiple servers
                // (assuming this message bus is persistent and distributed ala RabbitMQ)
                // Once it arrives on a server, we'll route the event there to be handled
                // by the consumers.
                _messageBusSpecifier
                    .DeclareExchange(AppEventConstants.EventExchange, ExchangeTypes.Direct)
                    .SpecifyExchange(AppEventConstants.EventExchange);
                _publisher.Initialize(AppEventConstants.EventExchange);
                _initialized = true;
            }

        }
    }

    /// <summary>
    /// ExecuteAsync gets app events from mongo database and publishes them to the specified message bus 
    /// </summary>
    /// <param name="stoppingToken">Cancellation token</param>
    /// <returns></returns>
    /// <summary>
    /// CAG RE
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        MaybeInitialize();

        while(!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if(!_topo.IsThisServerInRole(EventPumpRoleSpecifier.Name))
                {
                    await Task.Delay(2000, stoppingToken);
                    continue;
                }

                DateTime cutOff = DateTime.UtcNow;

                // get saved events ready to send
                var (toProcess, rc) = await _events.GetAllAsync(ev =>
                        ev.State == AppEventState.New || // new events
                        (ev.State == AppEventState.Reserved &&
                          ev.TakenExpiration < cutOff) && // those we tried to send before, but minutes have passed and it's still here
                          ev.DeferredUntil < DateTime.UtcNow);

                if(toProcess.Count == 0)
                {
                    // nothing to do yet
                    await Task.Delay(150, stoppingToken);
                    
                        continue;
                }    

                var sendBucket = new List<AppEvent>(); // events to send to the message bus
                var groomBucket = new List<AppEvent>(); // events to delete
                var batchTasks = new List<Task>(); // work to be done

                // reserve events to process.
                foreach (var @event in toProcess)
                {
                    @event.SendRetries += 1;

                    // see if we're giving up on this event
                    if (@event.State != AppEventState.New)
                    {
                        var age = DateTime.UtcNow - @event.TakenExpiration;
                        if(Math.Abs(age.TotalMinutes) > _tooAgedMinutes 
                            && @event.SendRetries > _retryCutOff)
                        {
                            // too old, too many retries, put it in the groom list
                            groomBucket.Add(@event);
                            continue;
                        }
                    }
                                        
                    // mark it reserved in the repo and then enqueue it for sending.
                    @event.State = AppEventState.Reserved;
                    var expireTime= DateTime.UtcNow.AddSeconds(_reenqueueTimeoutSeconds);
                    @event.TakenExpiration = expireTime;
                    var reservation = _events.UpsertIgnoreVersionAsync(@event);
                    batchTasks.Add(reservation);
                    sendBucket.Add(@event);
                } // process each event



                // add grooming to our work list
                if (groomBucket.Count > 0)
                {
                    var groomedIds = groomBucket.Select(it => it.Id);
                    var grooming = _events.DeleteBatchAsync(groomedIds);
                    batchTasks.Add(grooming);
                }

                // wait for our work to be done
                if(batchTasks.Count > 0)
                    await Task.WhenAll(batchTasks);
                batchTasks.Clear();

                // enqueue the events to the bus
                foreach (var @event in sendBucket)
                {
                    var topic = @event.Topic!;
                    topic.Guarantees().IsNotNullOrEmpty();

                    // Send these events to the AppEventBridge.
                    var enqueue = _publisher.SendAsync(@event, AppEventConstants.EventRoute);
                    batchTasks.Add(enqueue);
                }

                await Task.WhenAll(batchTasks);
                batchTasks.Clear();

                // we're assuming a persistent message bus.
                // (like RabbitMQ. Our demo memory bus breaks the assumption.)
                // Once they're all on the bus, we can remove them 
                // from the repo.
                var sentIds = sendBucket.Select(ev => ev.Id);
                await _events.DeleteBatchAsync(sentIds);


                await Task.Delay(50, stoppingToken);
            } 
            catch(OperationCanceledException)
            {
                continue; // go back the top of our loop to exit
            }
            catch(Exception ex)
            {
                // catch everything else to prevent the exception
                // from busting the app event pump.
                _alert.RaiseAlert(ApplicationAlertKind.System, LogLevel.Information,
                    ex.TraceInformation(), 2, nameof(AppEventPump));
            }
        }
    }
}
