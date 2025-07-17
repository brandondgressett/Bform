namespace BFormDomain.CommonCode.Platform.AppEvents;


/// <summary>
/// CAG RE
/// </summary>
public interface IAppEventConsumer
{
    /// <summary>
    /// CAG RE
    /// </summary>
    string Id { get; }
    /// <summary>
    /// CAG RE
    /// </summary>
    /// <param name="registrar"></param>
    /// <returns></returns>
    Task RegisterTopics(IAppEventConsumerRegistrar registrar);
    /// <summary>
    /// CAG RE
    /// </summary>
    /// <param name="event"></param>
    /// <param name="bindingId"></param>
    /// <returns></returns>
    Task ConsumeEvents(AppEvent @event, IEnumerable<string> bindingId);

}
