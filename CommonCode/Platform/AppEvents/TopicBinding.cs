namespace BFormDomain.CommonCode.Platform.AppEvents;


/// <summary>
/// CAG RE
/// </summary>
/// <param name="Consumer"></param>
/// <param name="Topic"></param>
/// <param name="BindingId"></param>
public record TopicBinding(IAppEventConsumer Consumer, string Topic, string BindingId);
