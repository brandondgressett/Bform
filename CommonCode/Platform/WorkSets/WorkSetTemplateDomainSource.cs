using BFormDomain.CommonCode.Platform.Content;

namespace BFormDomain.CommonCode.Platform.WorkSets;

public class WorkSetTemplateDomainSource : IContentDomainSource
{
    public ContentDomain Tell(IApplicationPlatformContent host)
    {
        return new ContentDomain
        {
            Name = nameof(WorkSetTemplate),
            Schema = host.LoadEmbeddedSchema<WorkSetTemplate>(),
            ContentType = typeof(WorkSetTemplate),
            InstanceGroupDescOrder = 50 
        };
    }
}
