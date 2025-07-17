using BFormDomain.CommonCode.Platform.Content;

namespace BFormDomain.CommonCode.Platform.Rules;

/// <summary>
/// Found by the IApplicationPlatformContent implementation,
/// used to help register all rule content instances and load their data.
/// </summary>
public class RuleContentDomainSource : IContentDomainSource
{
    public ContentDomain Tell(IApplicationPlatformContent host)
    {
        return new ContentDomain
        {
            Name = nameof(Rule),
            Schema = host.LoadEmbeddedSchema<Rule>(),
            ContentType = typeof(Rule),
            InstanceGroupDescOrder = 1000
        };
    }


    
}
