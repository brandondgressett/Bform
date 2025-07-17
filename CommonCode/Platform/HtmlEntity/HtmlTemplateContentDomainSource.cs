using BFormDomain.CommonCode.Platform.Content;

namespace BFormDomain.CommonCode.Platform.HtmlEntity;

public class HtmlTemplateContentDomainSource : IContentDomainSource
{
    public ContentDomain Tell(IApplicationPlatformContent host) => new ContentDomain
    {
        Name = nameof(HtmlTemplate),
        Schema = host.LoadEmbeddedSchema<HtmlTemplate>(),
        ContentType = typeof(HtmlTemplate),
        InstanceGroupDescOrder = 0
    };

}
