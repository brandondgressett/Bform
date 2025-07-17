using BFormDomain.CommonCode.Platform.Content;

namespace BFormDomain.CommonCode.Platform.Tables;

public class RegisteredTableQueryContentDomainSource : IContentDomainSource
{
    public ContentDomain Tell(IApplicationPlatformContent host)
    {
        return new ContentDomain
        {
            Name = nameof(RegisteredTableQueryTemplate),
            Schema = host.LoadEmbeddedSchema<RegisteredTableQueryTemplate>(),
            ContentType = typeof(RegisteredTableQueryTemplate)
        };
    }
}
