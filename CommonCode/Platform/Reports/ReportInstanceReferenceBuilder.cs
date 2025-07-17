using BFormDomain.CommonCode.Platform.Entity;
using System.Text;

namespace BFormDomain.CommonCode.Platform.Reports;

/// <summary>
/// ReportInstanceReferenceBuilderImplementation develops a URI for a Reportinstances by template name
///     -References:
///         >ReportInstance.cs
///         >ReportInstanceReferenceBuilder.cs
///         >RuleActionReportEnrollDashboard.cs
///     -Functions:
///         >MakeReference
/// </summary>
public static class ReportInstanceReferenceBuilderImplementation
{
    public static Uri MakeReference(string templateName, Guid id,
        bool template = false, bool vm = false, string? queryParameters = null)
    {
        var sb = new StringBuilder();
        sb.Append("bform://");
        sb.Append(nameof(ReportInstance));

        // only vms are available -- raw instances aren't that useful
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

        if (string.IsNullOrWhiteSpace(queryParameters))
        {
            sb.Append("?");
            sb.Append(queryParameters);
        }

        return new Uri(sb.ToString());
    }
}

public class ReportInstanceReferenceBuilder : IEntityReferenceBuilder
{
    public Uri MakeReference(string templateName, Guid id, 
        bool template = false, bool vm = false, string? queryParameters = null)
    {
        return ReportInstanceReferenceBuilderImplementation.MakeReference(templateName, id, template, vm, queryParameters);
    }
}
