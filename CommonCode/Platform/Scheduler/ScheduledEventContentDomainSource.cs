using BFormDomain.CommonCode.Platform.Content;

namespace BFormDomain.CommonCode.Platform.Scheduler;

public class ScheduledEventContentDomainSource : IContentDomainSource
{
    public ContentDomain Tell(IApplicationPlatformContent host)
    {
        return new ContentDomain
        {
            Name = nameof(ScheduledEventTemplate),
            Schema = host.LoadEmbeddedSchema<ScheduledEventTemplate>(),
            ContentType = typeof(ScheduledEventTemplate),
            InstanceGroupDescOrder = 20000
        };
    }
}
