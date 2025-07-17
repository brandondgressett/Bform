using BFormDomain.CommonCode.Platform.Entity;
using System.Text;

namespace BFormDomain.CommonCode.Platform.HtmlEntity;


public static class HtmlEntityReferenceBuilderImplementation
{
    public static Uri MakeReference(string templateName, Guid id, bool template = false, bool vm = false, string? queryParameters = null)
    {
        var sb = new StringBuilder();
        sb.Append("bform://");
        sb.Append(nameof(HtmlTemplate));
        sb.Append($"/{templateName}");
        return new Uri(sb.ToString());
    }
}

/// <summary>
/// HtmlEntityReferenceBuilder builds a URI by template name
///     -References:
///         >AddEntityReferenceBuilder.cs
///     -Functions:
///        >MakeReference
/// </summary>
public class HtmlEntityReferenceBuilder : IEntityReferenceBuilder
{
    public Uri MakeReference(string templateName, Guid id, bool template = false, bool vm = false, string? queryParameters = null)
    {
        return HtmlEntityReferenceBuilderImplementation.MakeReference(templateName, id, template, vm, queryParameters);
    }
}
