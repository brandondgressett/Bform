using BFormDomain.CommonCode.Platform.Content;

namespace BFormDomain.CommonCode.Platform.Tables;

public class TableTemplateContentDomainSource:IContentDomainSource
{
    public ContentDomain Tell(IApplicationPlatformContent host)
    {
        return new ContentDomain
        {
            Name = nameof(TableTemplate),
            Schema = host.LoadEmbeddedSchema<TableTemplate>(),
            ContentType = typeof(TableTemplate),
            InstanceGroupDescOrder = 7000
        };
    }
}
