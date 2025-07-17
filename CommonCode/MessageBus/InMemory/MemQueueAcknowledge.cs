using System.Collections.Concurrent;

namespace BFormDomain.MessageBus.InMemory;

internal class MemQueueAcknowledge : IMessageAcknowledge
{
    private readonly LightMessageQueueEnvelope _messageQueue;
    private readonly ConcurrentQueue<LightMessageQueueEnvelope> _parent;

    public MemQueueAcknowledge(ConcurrentQueue<LightMessageQueueEnvelope> parent, LightMessageQueueEnvelope messageQueue)
    {
        _parent = parent;
        _messageQueue = messageQueue;
    }

    #region IMessageAcknowledge Members

    public void MessageAcknowledged()
    {
    }

    public void MessageAbandoned()
    {
        _parent.Enqueue(_messageQueue);
    }

    public void MessageRejected()
    {
    }

    #endregion
}
