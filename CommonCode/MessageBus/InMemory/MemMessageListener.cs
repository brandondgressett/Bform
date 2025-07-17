using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace BFormDomain.MessageBus.InMemory;

/// <summary>
/// MemMessageListener implements Listen() from IMessageListener to handle messages from the message bus defined. 
///     -Usage (Injected with IMessageListener):
///         >AppEventBridge.cs
///         >NotificationService.cs
///         >ConsolidationService.cs
///         >DigestResultReceiver.cs
///         >DuplicateSuppressionService.cs
///         >SuppressionResultReceiver.cs
///     -Functions:
///         >Initialize: 
///         >public Listen: 
///         >private Listen:
///         >OnListenAbort:
///         >Reenqueue:
///         >Dispose:
/// </summary>
public class MemMessageListener : IMessageListener
{
    private readonly IMessageBusSpecifier _bus;
    private ConcurrentQueue<LightMessageQueueEnvelope>? _q;
    private ManualResetEventSlim? _sev;

    private readonly ConcurrentDictionary<Type, Action<object, CancellationToken, IMessageAcknowledge>> _sinks =
        new();

    private CancellationToken _ct;
    private CancellationTokenSource _cts;
    private bool _isDisposed;
    private Task? _listening;
    private readonly ILogger<MemMessageListener> _log;
    private readonly IApplicationAlert _alerts;

    private volatile bool _paused = false;

    private string? _exchange;
    private string? _name;

    public bool Paused
    {
        get { return _paused; }
        set { _paused = true; }
    }

    public event EventHandler<IEnumerable<object>>? ListenAborted;


    public MemMessageListener(
        IMessageBusSpecifier bus, 
        ILogger<MemMessageListener> logger, 
        IApplicationAlert alerts)
    {
        _alerts = alerts;
        _bus = bus;
        _cts = new CancellationTokenSource();
        _log = logger;
    }

    #region IMessageListener Members

    public void Initialize(string exchangeName, string qName)
    {
        _exchange = exchangeName;
        _name = qName;
        var qs = _bus.SpecifyExchange(exchangeName).SpecifyQueue(qName);
        _q = ((IMemQueueAccess)qs).Queue;
        _sev = ((IMemQueueAccess)qs).SentEvent;
    }

    public void Listen(params KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>[] listener)
    {
        _q!.Requires("Message Queue Listener NOT initialized before listening").IsNotNull();
        listener.Requires().IsNotEmpty();

        // we're already listening to another station
        // clean up and start over
        if (_listening is not null)
        {
            _cts.Cancel();
            _listening.Wait(10000);
            _listening.Dispose();

            _cts = new CancellationTokenSource();
        }
        _sinks.Clear();

        // add the user's listener params to our sinks collection
        foreach (var sink in listener)
        {
            _sinks[sink.Key] = sink.Value;
        }

        if(!_sinks.Any())
        {
            _log.LogError("Listener has no sinks!");
        }

        _ct = _cts.Token;

        _listening = Task.Run(() => Listen(this), _ct); 
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            _cts.Cancel();
            _listening?.Wait();
            _cts.Dispose();
            _listening?.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    #endregion

    protected virtual void OnListenAborted()
    {
        var pending = _q?.Select(it => (object)it.Data);
        ListenAborted?.Invoke(this, pending!);
    }

    private static void Listen(object that)
    {
        var @this = (MemMessageListener)that;
        if(!@this._sinks.Any())
        {
            @this._log.LogError("Listener has no sinks!");
        }
        

        try
        {
            while (!@this._ct.IsCancellationRequested)
            {

                try
                {



                    if (@this._paused)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    bool sent = true;
                    sent = @this._sev!.Wait(1000, @this._ct);

                    if (!@this._ct.IsCancellationRequested)
                    {
                        try
                        {
                            LightMessageQueueEnvelope env;
                            var messageRead = @this._q!.TryDequeue(out env!);
                            while (messageRead)
                            {
                                var msg = env!.Decode();
#if DEBUG
                                @this._log.LogInformation("Listener on {ex}.{q} received:{m}",
                                    @this._exchange, @this._name, JsonConvert.SerializeObject(msg, Formatting.Indented));
#endif

                                if (@this._sinks.ContainsKey(env.MessageType))
                                {
                                    var sink = @this._sinks[env.MessageType];
                                    sink(msg, @this._cts.Token, new MemQueueAcknowledge(@this._q, env));
                                } else
                                {
#if DEBUG
                                    @this._log.LogInformation("Listener on {ex}.{q} no sink for message:{t}\n{m}",
                                        @this._exchange, @this._name, msg.GetType().GetFriendlyTypeName(),
                                        JsonConvert.SerializeObject(msg, Formatting.Indented));
                                    if (@this._sinks.Keys.Any())
                                    {
                                        @this._log.LogInformation("message type: {tp}. Existing sinks:", msg.GetType().GetFriendlyTypeName());
                                        foreach (var s in @this._sinks.Keys)
                                        {
                                            @this._log.LogInformation("sink: {k}", s.GetFriendlyTypeName());
                                        }
                                    } else
                                    {
                                        @this._log.LogError("listener has no sinks!");
                                    }
#endif
                                }

                                messageRead = @this._q.TryDequeue(out env!);
                            }
                        }
                        catch (Exception ex)
                        {
                            var es = ex.TraceInformation();
                            @this._log.LogError(es);
                        }

                        @this._sev.Reset();
                    }

                }
                catch (ThreadAbortException)
                {
                    @this.OnListenAborted();
                }
                catch (OperationCanceledException)
                {
                    @this.OnListenAborted();
                }
                catch (Exception e)
                {
                    
                    @this._alerts.RaiseAlert(ApplicationAlertKind.System, LogLevel.Error, $"Unhandled exception in message bus listener: {e.TraceInformation()}");
                }

            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    internal void Reenqueue(LightMessageQueueEnvelope messageQueue)
    {
        _q!.Enqueue(messageQueue);
        (_q as IMemQueueAccess)!.SentEvent.Set();
    }
}
