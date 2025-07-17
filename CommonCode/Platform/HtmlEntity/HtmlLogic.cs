using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.Validation;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.HtmlEntity;

/// <summary>
/// HtmlLogic gets html entity by template name
///     -References:
///         >HtmlEntityLoaderModule.cs
///     -Functions:
///        >GetHtml
/// </summary>
public class HtmlLogic
{
    private readonly IApplicationPlatformContent _content;

    public HtmlLogic(IApplicationPlatformContent content)
    {
        _content = content;
    }

    public HtmlInstance GetHtml(string templateName)
    {
        var template = _content.GetContentByName<HtmlTemplate>(templateName)!;
        template.Requires().IsNotNull();

        return new HtmlInstance
        {
            Content = template.Content,
            CreatedDate = DateTime.Now,
            Creator = Constants.BuiltIn.SystemUser,
            EntityType = nameof(HtmlInstance),
            HostWorkItem = Constants.BuiltIn.SystemWorkItem,
            HostWorkSet = Constants.BuiltIn.SystemWorkSet,
            Id = Guid.NewGuid(),
            LastModifier = Constants.BuiltIn.SystemUser,
            Template = template.Name,
            UpdatedDate = DateTime.Now,
            Tags = template.Tags
        };
    }

}
