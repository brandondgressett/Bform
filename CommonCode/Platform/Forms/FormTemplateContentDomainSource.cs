using BFormDomain.CommonCode.Platform.Content;

namespace BFormDomain.CommonCode.Platform.Forms;

/// <summary>
/// Found by the IApplicationPlatformContent implementation,
/// used to help register all form template content instances and load their data.
/// </summary>
public class FormTemplateContentDomainSource: IContentDomainSource
{

   


    public ContentDomain Tell(IApplicationPlatformContent host)
    {
        return new ContentDomain
        {
            Name = nameof(FormTemplate),
            Schema = host.LoadEmbeddedSchema<FormTemplate>(),
            ContentType = typeof(FormTemplate),
            InstanceGroupDescOrder = 11000
        };
    }

    
}
