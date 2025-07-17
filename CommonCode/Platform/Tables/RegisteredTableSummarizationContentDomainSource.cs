using BFormDomain.CommonCode.Platform.Content;

namespace BFormDomain.CommonCode.Platform.Tables;

public class RegisteredTableSummarizationContentDomainSource: IContentDomainSource
{

    public ContentDomain Tell(IApplicationPlatformContent host)
    {
        return new ContentDomain
        {
            Name = nameof(RegisteredTableSummarizationTemplate),
            Schema = host.LoadEmbeddedSchema<RegisteredTableSummarizationTemplate>(),
            ContentType = typeof(RegisteredTableSummarizationTemplate)
        };
    }

}
