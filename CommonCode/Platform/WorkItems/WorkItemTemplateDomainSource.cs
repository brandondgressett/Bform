using BFormDomain.CommonCode.Platform.Content;

namespace BFormDomain.CommonCode.Platform.WorkItems;

public class WorkItemTemplateDomainSource : IContentDomainSource
{
    public ContentDomain Tell(IApplicationPlatformContent host)
    {
        return new ContentDomain
        {
            Name = nameof(WorkItemTemplate),
            Schema = host.LoadEmbeddedSchema<WorkItemTemplate>(),
            ContentType = typeof(WorkItemTemplate),
            InstanceGroupDescOrder = 40000
        };
    }
}
