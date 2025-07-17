using BFormDomain.HelperClasses;
using System.Globalization;

namespace BFormDomain.MessageBus;

/// <summary>
///  MessageExchangeDeclaration tracks the information about an exchange upon an exchanges creation
///     -References:
///         >MemExchange
///         >MemMessagePublisher
///         >AppEventDistriber
///         >TopicRegistration
///         >MemMessageBus 
///     -Functions:
///         >BindMessageToQueues
///         >TopicMatch
/// </summary>
public class MessageExchangeDeclaration
{
    public MessageExchangeDeclaration()
    {
        Queues = new List<MessageQueueDeclaration>();
    }


    public string Name { get; set; } = "";

    public ExchangeTypes Type { get; set; }

    public List<MessageQueueDeclaration> Queues { get; private set; }

    /// <summary>
    /// BindMessageToQueues binds messages to queue by exchange type and by key
    /// </summary>
    /// <param name="key">Key to find queue</param>
    /// <param name="ex">Enum defining exchange types</param>
    /// <param name="qs">List of queues</param>
    /// <returns></returns>
    public static IEnumerable<string> BindMessageToQueues(string key, ExchangeTypes ex,
                                                          IEnumerable<IQueueSpecifier> qs)
    {
        switch (ex)
        {
            case ExchangeTypes.Direct:
                {
                    var routeBound =
                        qs.Where(
                            q =>
                            !q.Bindings.Any() ||
                            q.Bindings.Any(k => string.Compare(key, k, true, CultureInfo.InvariantCulture) == 0));

                    if (routeBound.Count() == 1)
                        return EnumerableEx.OfOne(routeBound.First().Name);

                    if (routeBound.Any())
                    {
                        var matches = (from q in routeBound
                                       select q.Name).Distinct().Shuffle(GoodSeedRandom.Create());


                        return matches.Take(1);
                    }

                    return Enumerable.Empty<string>();
                }
                //break;

            case ExchangeTypes.Fanout:
                {
                    return qs.Select(q => q.Name);
                }
                //break;

            case ExchangeTypes.Topic:
                {
                    return (from q in qs
                            where q.Bindings.Any(k => TopicMatch(key, k))
                            select q.Name).Distinct();
                }
                //break;
        }

        return Enumerable.Empty<string>();
    }

    /// <summary>
    /// TopicMatch returns whether or not bindings exist 
    /// </summary>
    /// <param name="messageKey">Key to search for message by</param>
    /// <param name="binding"></param>
    /// <returns></returns>
    public static bool TopicMatch(string messageKey, string binding)
    {
        if (string.IsNullOrEmpty(messageKey) || string.IsNullOrEmpty(binding))
            return false;

        if (binding == "#")
            return true;

        var keyParts = messageKey.Split('.');
        var bindingParts = binding.Split('.');


        for (var eachPart = 0; eachPart != keyParts.Length; eachPart++)
        {
            var key = keyParts[eachPart];

            if (eachPart > bindingParts.Length)
                return false;

            var binder = bindingParts[eachPart];
            if (binder == "*")
                continue;

            if (binder == "#")
                break;

            if (string.Compare(binder, key, false, CultureInfo.InvariantCulture) != 0)
                return false;
        }

        return true;
    }
}
