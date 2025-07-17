using BFormDomain.CommonCode.Platform.Entity;
using System.Text;

namespace BFormDomain.CommonCode.Platform.Forms;




/// <summary>
/// FormInstanceReferenceBuilderImplementation develops URIs for form instances
///     -Usage
///         >FormInstance.cs
///         >RuleActionFormEnrollDashboard.cs
///     -Functions
///         >MakeReference
/// </summary>
public static class FormInstanceReferenceBuilderImplementation
{
    public static Uri MakeReference(string templateName, Guid id, bool template = false, bool vm= false, string? queryParameters=null)
    {
        var sb = new StringBuilder();
        sb.Append("bform://");
        sb.Append(nameof(FormInstance));

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

public class FormInstanceReferenceBuilder : IEntityReferenceBuilder
{
    public Uri MakeReference(string templateName, Guid id, bool template = false, bool vm = false, string? queryParameters=null)
    {
        return FormInstanceReferenceBuilderImplementation.MakeReference(templateName,id,template,vm,queryParameters);
    }
}

