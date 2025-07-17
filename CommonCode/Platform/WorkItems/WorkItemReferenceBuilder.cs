using BFormDomain.CommonCode.Platform.Entity;
using System.Text;

namespace BFormDomain.CommonCode.Platform.WorkItems;


public static class WorkItemReferenceBuilderImplementation
{
    public static Uri MakeReference(string templateName, Guid id, bool template = false, bool vm = false, string? queryParameters = null)
    {
        var sb = new StringBuilder();
        sb.Append("bform://");
        sb.Append(nameof(WorkItem));

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


public class WorkItemReferenceBuilder : IEntityReferenceBuilder
{
    public Uri MakeReference(string templateName, Guid id, bool template = false, bool vm = false, string? queryParameters = null)
    {
        return WorkItemReferenceBuilderImplementation.MakeReference(templateName, id, template, vm, queryParameters);   
    }
}
