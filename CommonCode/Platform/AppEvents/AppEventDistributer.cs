using BFormDomain.CommonCode.Utility.CompletionTracking;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.MessageBus;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.AppEvents;

/// <summary>
/// Distributes routed events to IAppEventConsumers.
/// Register as singleton, as type AppEventDistributer without the interfaces it supports.
/// </summary>
public class AppEventDistributer : IAppEventConsumerRegistrar // no need to register the interface with DI
{
    private readonly List<IAppEventConsumer> _consumers;
    private readonly object _initLock = new();
    private bool _isInitialized = false;
    private readonly ConcurrentBag<TopicBinding> _bindings = new();
    private readonly IApplicationAlert _alerts;
    private readonly ITrackWorking _tracker;
    private readonly UserActionCompletion _userActionCompletion;
    
    public AppEventDistributer(
            IEnumerable<IAppEventConsumer> consumers,
            ITrackWorking tracker,
            UserActionCompletion userActionCompletion,
            IApplicationAlert alerts)
    {
        _consumers = consumers.ToList();
        _alerts = alerts;
        _tracker = tracker;
        _userActionCompletion = userActionCompletion;
    }

    /// <summary>
    /// CAG RE
    /// </summary>
    public void MaybeInitialize()
    {
        if (_isInitialized)
            return;

        lock (_initLock)
        {
            if (!_isInitialized)
            {
                var work = new List<Task>();
                foreach (var consumer in _consumers)
                    work.Add(consumer.RegisterTopics(this));

                AsyncHelper.RunSync(()=> Task.WhenAll(work));

                _isInitialized = true;
            }
        }
    }

    /// <summary>
    /// CAG RE
    /// </summary>
    /// <param name="event"></param>
    /// <param name="ack"></param>
    /// <returns></returns>
    public async Task DistributeEvent(AppEvent @event, IMessageAcknowledge ack)
    {
        try
        {
            MaybeInitialize();
            var po = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

            await Parallel.ForEachAsync(_consumers, po, async (consumer, ct) =>
            {
                var matches = _bindings
                    .AsParallel()
                    .Where(it => Object.ReferenceEquals(it.Consumer, consumer)
                                && MessageExchangeDeclaration.TopicMatch(@event.Topic!, it.Topic))
                    .Select(it => it.BindingId);

                if (!ct.IsCancellationRequested)
                {

                    if (matches.Any())
                    {
                        try
                        {
                            await consumer.ConsumeEvents(@event, matches);
                        }
                        catch (Exception ex)
                        {
                            _alerts.RaiseAlert(ApplicationAlertKind.General, Microsoft.Extensions.Logging.LogLevel.Error,
                                ex.TraceInformation());
                        }
                    }
                }
            });

            ack.MessageAcknowledged();

            await MaybeSignalUserActionCompleted(@event);

        }
        catch (Exception exc)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.General, Microsoft.Extensions.Logging.LogLevel.Error,
                exc.TraceInformation());
            ack.MessageRejected();
        }
    }

    /// <summary>
    /// CAG RE
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    private async Task MaybeSignalUserActionCompleted(AppEvent @event)
    {
        if (!string.IsNullOrWhiteSpace(@event.ActionId) &&
               @event.OriginUser is not null)
        {
            await _tracker.DecrementWork(@event.ActionId);

            if (await _tracker.MaybeCompleteWork(@event.ActionId))
            {
                // action is completed, send the signal.
                await _userActionCompletion.SignalComplete(@event.OriginUser.Value, @event.ActionId);
            }
        }
    }

    /// <summary>
    /// CAG RE
    /// </summary>
    /// <param name="binding"></param>
    public void RegisterTopic(TopicBinding binding)
    {
        _bindings.Add(binding);
    }
}
