using System.Collections.Concurrent;

namespace BFormDomain.MessageBus.InMemory;

internal interface IMemQueueAccess
{
    ConcurrentQueue<LightMessageQueueEnvelope> Queue { get; }
    ManualResetEventSlim SentEvent { get; }
}
