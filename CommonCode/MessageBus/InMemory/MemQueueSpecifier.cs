using System.Collections.Concurrent;

namespace BFormDomain.MessageBus.InMemory;

/// <summary>
/// MemQueueSpecifier implements IQueueSpecifier and IMemQueueAccess to define the name of a queue, what messages are bound to it, and which events have access to which queues. 
///     -References:
///         >MemExchange.cs
///     -Functions:
///         >Dispose()
/// </summary>
internal class MemQueueSpecifier : IQueueSpecifier, IMemQueueAccess, IDisposable
{

    

    private readonly ConcurrentQueue<LightMessageQueueEnvelope> _queue =
        new();

    private readonly ManualResetEventSlim _queueEvent;

    private readonly MessageQueueDeclaration _queueSpec;

    private bool _isDisposed;

    public MemQueueSpecifier(MessageQueueDeclaration queueSpec)
    {
       
        _queueSpec = queueSpec;
        _queueEvent = new ManualResetEventSlim(false);
    }

    #region IMemQueueAccess Members

    public ConcurrentQueue<LightMessageQueueEnvelope> Queue
    {
        get { return _queue; }
    }

    public ManualResetEventSlim SentEvent
    {
        get { return _queueEvent; }
    }

    #endregion

    #region IQueueSpecifier Members

    public string Name
    {
        get { return _queueSpec.Name; }
    }

    public IEnumerable<string> Bindings
    {
        get { return _queueSpec.Bindings; }
    }

    #endregion

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            _queueEvent.Dispose();
        }
    }
}
