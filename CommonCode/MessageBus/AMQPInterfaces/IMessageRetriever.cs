namespace BFormDomain.MessageBus;

public interface IMessageRetriever: IDisposable
{
    void Initialize(string exchangeName, string qName);
    Task<MessageContext<T>?> MaybeGetMessageAsync<T>()
        where T: class,new();
}


