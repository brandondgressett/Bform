using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Utility;
using BFormDomain.CommonCode.Utility.CompletionTracking;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Newtonsoft.Json;
using BFormDomain.CommonCode.Platform.ManagedFiles;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.AppEvents;

/// <summary>
/// CAG RE
/// </summary>
/// <summary>
/// AppEventSink adds events to mongo database for event pump to publish to message bus
///     -References:
///         >RegistrationLogic.cs
///         >UserManagementLogic.cs
///         >CommentLogic.cs
///         >FormLogic.cs
///         >KPILogic.cs
///         >ManagedFileLogic.cs
///         >RuleLogic.cs
///         >RuleActionCustomEvent.cs
///         >SinkEventJob.cs
///         >TableLogic.cs
///         >WorkItemLogic.cs
///         >WorkSetLogic.cs
///     -Functions:
///         >Enqueue
///         >MaybeTrackUserAction
///         >BeginBatch
///         >CommitBatch
/// </summary>
public class AppEventSink
{
    private readonly IRepository<AppEvent> _appEventsRepository;
    private readonly IApplicationAlert _alerts;
    private readonly ILogger<AppEventSink> _logger;
    private readonly AppEventSinkOptions _options;
    private readonly TopicRegistrations _registrations;
    private readonly ITrackWorking _tracker;
    private readonly TenantAwareEventFactory _eventFactory;

    private readonly List<AppEvent> _currentBatch = new();

    private ITransactionContext? _transaction;
    

    public AppEventSink(
        IRepository<AppEvent> repository,
        IApplicationAlert alerts,
        TopicRegistrations registrations,
        ITrackWorking tracker,
        TenantAwareEventFactory eventFactory,
        ILogger<AppEventSink> logger,
        IOptions<AppEventSinkOptions> options)
    {
        _alerts = alerts;
        _logger = logger;
        _appEventsRepository = repository;
        _registrations = registrations;
        _options = options.Value;
        _tracker = tracker;
        _eventFactory = eventFactory ?? throw new ArgumentNullException(nameof(eventFactory));
    }

    /// <summary>
    /// Enqueue adds app events to mongo database
    /// </summary>
    /// <param name="origin">Where the action originated</param>
    /// <param name="topic">Whihc topic to match to</param>
    /// <param name="action">What action was performed</param>
    /// <param name="entity"></param>
    /// <param name="userId">Which user performed action</param>
    /// <param name="addTags"></param>
    /// <param name="seal"></param>
    /// <param name="deferUntil">Halts an event until defered time</param>
    /// <returns></returns>
    public async Task Enqueue(
        AppEventOrigin? origin,
        string topic,
        string? action,
        IAppEntity entity,
        Guid? userId,
        IEnumerable<string>? addTags,
        bool seal = false,
        DateTime? deferUntil = null)
    {
        _transaction.Requires().IsNotNull();
        topic.Requires().IsNotNullOrEmpty();

        // check if anything is watching for this
        if (!_registrations.IsRegistered(topic))
        {
            if(_options.DebugEvents)
                _logger.LogDebug("No registered listener for topic {Topic}", topic);
            return;
        }

        // Check for sealed parent events
        if (origin?.Preceding?.Seal == true)
        {
            if (_options.DebugEvents)
                _logger.LogInformation("Event with topic {Topic} is sealed by {Generator}", topic, origin.Generator ?? "unknown");
            return;
        }

        // Check generation limit
        if (origin?.Preceding != null)
        {
            var generation = origin.Preceding.EventGeneration + 1;
            if (generation >= _options.GenerationLastLimit)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.System, LogLevel.Critical,
                    $"Event cascade past generation limit! Event line: {origin.Preceding.EventLine}. Initiated {origin.Generator}, {entity.Template ?? "unknown"} {entity.Id}");
                
                if(_options.DebugEvents)
                {
                    _logger.LogWarning("Event cascade past generation limit! Initiated: {Generator}, {Template}", origin.Generator, entity.Template);
                }
                _currentBatch.Clear();
                return;
            }
        }

        var jsonentity = entity.ToJson();
        var jsontext = jsonentity.ToString();
        var bsonentity = jsonentity.ToBsonObject();

        // Use the factory to create the event with proper tenant context
        var appEvent = _eventFactory.CreateEvent(
            topic: topic,
            action: action,
            entity: entity,
            userId: userId,
            origin: origin);

        // Apply additional properties not handled by factory
        appEvent.EntityPayload = bsonentity;
        appEvent.DeferredUntil = deferUntil ?? DateTime.MinValue;
        appEvent.Seal = seal;

        // Add any additional tags
        if (addTags is not null)
        {
            foreach (var at in addTags)
                if (!appEvent.Tags.Contains(at))
                    appEvent.Tags.Add(at);
        }

      

        if(_options.DebugEvents)
        {
            var rv = appEvent.ToRuleView();
            _logger.LogInformation("{eventJson}", rv);

        }

        _currentBatch.Add(appEvent);

        await MaybeTrackUserAction(action, userId, true);
    }


  
    /// <summary>
    /// 
    /// </summary>
    /// <param name="action">Action performed</param>
    /// <param name="userId">USer that performed action</param>
    /// <param name="isNatural"></param>
    /// <returns></returns>
    /// <summary>
    /// CAG RE
    /// </summary>
    /// <param name="action"></param>
    /// <param name="userId"></param>
    /// <param name="isNatural"></param>
    /// <returns></returns>
    private async Task MaybeTrackUserAction(string? action, Guid? userId, bool isNatural)
    {
        if (userId is not null && !string.IsNullOrEmpty(action))
        {
            if (isNatural)
            {
                await _tracker.BeginWork(action, TimeSpan.FromSeconds(_options.ActionTrackingExpirationSeconds), 1);
            }
            else
                await _tracker.IncrementWork(action);
        }
    }

    /// <summary>
    /// CAG RE
    /// </summary>
    /// <param name="tctx"></param>
    /// <summary>
    /// 
    /// </summary>
    /// <param name="tctx"></param>
    public void BeginBatch(ITransactionContext tctx)
    {
        _currentBatch.Clear();
        _transaction = tctx;
    }

    /// <summary>
    /// CAG RE
    /// </summary>
    /// <returns></returns>
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task CommitBatch()
    {
        _transaction.Requires().IsNotNull();

        if (_currentBatch.Any())
        {
            await _appEventsRepository.CreateBatchAsync(_transaction!, _currentBatch);
        }

        _currentBatch.Clear();
        _transaction = null!;
    }


}
