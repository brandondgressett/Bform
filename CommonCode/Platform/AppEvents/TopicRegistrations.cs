using BFormDomain.MessageBus;
using System.Collections.Concurrent;

namespace BFormDomain.CommonCode.Platform.AppEvents;

/// <summary>
/// Used to keep track of which topics event consumers are watching for.
/// If an event doesn't match any of them, it won't be enqueued.
/// Must be registered as a singleton.
/// </summary>
public class TopicRegistrations
{

    /// <summary>
    /// CAG RE
    /// </summary>
    private readonly ConcurrentDictionary<string,byte> _topicAudience = new(); // there's no concurrent hash set. Value is irrelevant.
   
   
    public TopicRegistrations()
    {
       
    }

    /// <summary>
    /// CAG RE
    /// </summary>
    /// <param name="topic"></param>
    public void Register(string topic)
    {
        _topicAudience[topic] = 0;
    }


    /// <summary>
    /// CAG RE
    /// </summary>
    /// <param name="topic"></param>
    /// <returns></returns>
    public bool IsRegistered(string topic)
    {
        return _topicAudience.Keys.Any(key =>
            MessageExchangeDeclaration.TopicMatch(topic, key));
    }




}
