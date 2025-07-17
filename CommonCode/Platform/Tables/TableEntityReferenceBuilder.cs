using BFormDomain.CommonCode.Platform.Entity;
using System.Text;

namespace BFormDomain.CommonCode.Platform.Tables;


public static class TableEntityReferenceBuilderImplementation
{
    public static Uri MakeReference(
        string templateName, 
        Guid id, 
        bool template = false, 
        bool vm = false, 
        string? queryParameters = null)
    {
        var sb = new StringBuilder();
        sb.Append("bform://");
        sb.Append(nameof(TableTemplate));

        // only vms are available -- raw instances aren't that useful
        sb.Append("/vm/");
        sb.Append("/Template/");
        sb.Append(templateName);
        
        if (string.IsNullOrWhiteSpace(queryParameters))
        {
            sb.Append("?");
            sb.Append(queryParameters);
        }

        return new Uri(sb.ToString());
    }
}

public class TableEntityReferenceBuilder : IEntityReferenceBuilder
{
    public Uri MakeReference(
        string templateName, 
        Guid id, 
        bool template = false, 
        bool vm = false, 
        string? queryParameters = null)
    {
        return TableEntityReferenceBuilderImplementation.MakeReference(
            templateName, id, template, vm, queryParameters);    
    }
}
