using BFormDomain.CommonCode.Platform.Content;

namespace BFormDomain.CommonCode.Platform.Reports;

public class ReportTemplateContentDomainSource : IContentDomainSource
{
    public ContentDomain Tell(IApplicationPlatformContent host)
    {
        return new ContentDomain
        {
            Name = nameof(ReportTemplate),
            Schema = host.LoadEmbeddedSchema<ReportTemplate>(),
            ContentType = typeof(ReportTemplate),
            InstanceGroupDescOrder = 6000
        };
    }
}
