using BFormDomain.CommonCode.Platform.Content;

namespace BFormDomain.CommonCode.Platform.KPIs;

public class KPITemplateContentDomainSource: IContentDomainSource
{

    public ContentDomain Tell(IApplicationPlatformContent host)
    {
        return new ContentDomain
        {
            Name= nameof(KPITemplate),
            Schema = host.LoadEmbeddedSchema<KPITemplate>(),
            ContentType = typeof(KPITemplate),
            InstanceGroupDescOrder = 5000
        };
    }

}
