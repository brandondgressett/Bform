namespace BFormDomain.CommonCode.Platform.AppEvents;


/// <summary>
/// CAG RE
/// </summary>
public interface IAppEventConsumerRegistrar
{

    /// <summary>
    /// CAG RE
    /// </summary>
    /// <param name="binding"></param>
    void RegisterTopic(TopicBinding binding);
}
