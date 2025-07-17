using BFormDomain.HelperClasses;
using BFormDomain.Validation;
using System.Collections.Concurrent;

namespace BFormDomain.MessageBus.InMemory;

/// <summary>
///  MemExchange implements 
///     -References:
///         >IMessageBusSpecifier
///         >MemExchange
///         >MemMessageBus
///     -Functions:
///         >DeclareQueue
///         >DeleteQueue
///         >SpecifyQueue
///         >GetCachedSpecifier
/// </summary>
internal class MemExchange : IExchangeSpecifier
{
    private readonly MessageExchangeDeclaration _declSpec;
    

    private readonly ConcurrentDictionary<string, MemQueueSpecifier> _queueSpecifiers =
        new();

    internal MemExchange(MessageExchangeDeclaration declSpec)
    {
        _declSpec = declSpec;
    }

    #region IExchangeSpecifier Members

    public string Name
    {
        get { return _declSpec.Name; }
    }

    public ExchangeTypes ExchangeType
    {
        get { return _declSpec.Type; }
    }

    /// <summary>
    /// DeclareQueue adds a new queue to the 'Queues' list in the MemExchangeDeclaration instance
    /// </summary>
    /// <param name="queueName">Name of the queue</param>
    /// <param name="boundRoutes">Array of routes the queue is bound to</param>
    /// <returns></returns>
    public IExchangeSpecifier DeclareQueue(string queueName, params string[] boundRoutes)
    {
        if (_declSpec.Queues.Any(dsq => dsq.Name == queueName))
        {
            return this;
        }

        var q = new MessageQueueDeclaration { Name = queueName };
        q.Bindings.AddRange(boundRoutes);
        _declSpec.Queues.Add(q);

        return this;
    }

    /// <summary>
    /// DeclareQueue adds a new queue to the 'Queues' list in the MemExchangeDeclaration instance
    /// </summary>
    /// <param name="queueName">Name of the queue</param>
    /// <param name="boundRoutes">Array of routes the queue is bound to</param>
    /// <returns></returns>
    public IExchangeSpecifier DeclareQueue(Enum queueName, params string[] boundRoutes)
    {
        return DeclareQueue(queueName.EnumName(), boundRoutes);
    }

    /// <summary>
    /// DeleteQueue removes an instance of MessageQueueDeclaration from current instance of 'MemExchangeDeclaration.Queues' by queue name
    /// </summary>
    /// <param name="queueName">Name of queue</param>
    /// <returns></returns>
    public IExchangeSpecifier DeleteQueue(string queueName)
    {
        var item = _declSpec.Queues.FirstOrDefault(q => q.Name == queueName);
        if (null == item)
            return this;

        _declSpec.Queues.Remove(item);
        MemQueueSpecifier mqs;
        if (_queueSpecifiers.TryRemove(queueName, out mqs!))
            mqs.Dispose();

        return this;
    }

    /// <summary>
    /// DeleteQueue removes an instance of MessageQueueDeclaration from current instance of 'MemExchangeDeclaration.Queues' by queue name
    /// </summary>
    /// <param name="queueName">Name of queue</param>
    /// <returns></returns>
    public IExchangeSpecifier DeleteQueue(Enum queueName)
    {
        return DeleteQueue(queueName.EnumName());
    }

    /// <summary>
    /// SpecifyQueue returns cached version of MessageQueueDeclaration by queue name
    /// </summary>
    /// <param name="queueName">Name of queue</param>
    /// <returns></returns>
    public IQueueSpecifier SpecifyQueue(string queueName)
    {
        var item = _declSpec.Queues.FirstOrDefault(q => q.Name == queueName);
        
        item.Guarantees().IsNotNull();

        return GetCachedSpecifier(item!);
    }

    /// <summary>
    /// SpecifyQueue returns cached version of MessageQueueDeclaration by queue name
    /// </summary>
    /// <param name="queueName">Name of queue</param>
    /// <returns></returns>
    public IQueueSpecifier SpecifyQueue(Enum queueName)
    {
        return SpecifyQueue(queueName.EnumName());
    }


    public IEnumerable<IQueueSpecifier> Queues
    {
        get { return _declSpec.Queues.Select(GetCachedSpecifier); }
    }

    private MemQueueSpecifier GetCachedSpecifier(MessageQueueDeclaration queue)
    {
        MemQueueSpecifier mqs;
        if (!_queueSpecifiers.TryGetValue(queue.Name, out mqs!))
        {
            mqs = new MemQueueSpecifier(queue);
            if (!_queueSpecifiers.TryAdd(queue.Name, mqs))
            {
                mqs = _queueSpecifiers[queue.Name];
            }
        }

        return mqs;
    }

    #endregion
}
