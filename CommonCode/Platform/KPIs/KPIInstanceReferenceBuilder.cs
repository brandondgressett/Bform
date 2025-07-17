using BFormDomain.CommonCode.Platform.Entity;
using System.Text;

namespace BFormDomain.CommonCode.Platform.KPIs;


/// <summary>
/// KPIInstanceReferenceBuilderImplementation develops a URI for a KPI instance by template name
///     -References:
///         >FormInstanceReferenceBuilderImplementation.cs
///         >HtmlEntityReferenceBuilder.cs
///         >KPIInstanceReferenceBuilder.cs
///         >ManagedFileReferenceBuilder.cs
///         >ReportInstanceReferenceBuilder.cs
///         >TableEntityReferenceBuilder.cs
///         >WorkItemReferenceBuilder.cs
///         >WorkSetReferenceBuilder.cs
///     -Funtions:
///         >MakeReference
/// </summary>
public static class KPIInstanceReferenceBuilderImplementation
{
    public static Uri MakeReference(string templateName, Guid id, bool template = false, bool vm = false, string? queryParameters = null)
    {
        var sb = new StringBuilder();

       
        sb.Append("bform://");
        sb.Append(nameof(KPIInstance));

        if (vm)
            sb.Append("/vm/");

        if (template)
        {
            sb.Append("/Template/");
            sb.Append(templateName);
        }
        else
        {
            sb.Append(id);
        }

        return new Uri(sb.ToString());
    }
}

public class KPIInstanceReferenceBuilder : IEntityReferenceBuilder
{
    public Uri MakeReference(string templateName, Guid id, bool template = false, bool vm = false, string? queryParameters = null)
    {
        return KPIInstanceReferenceBuilderImplementation.MakeReference(templateName, id, template, vm, queryParameters);
    }
}
