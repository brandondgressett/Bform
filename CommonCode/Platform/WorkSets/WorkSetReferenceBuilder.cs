using BFormDomain.CommonCode.Platform.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Platform.WorkSets;



/// <summary>
/// WorkSetReferenceBuilderImplementation develops a URI for a WorkSet instance by template name
///     -References:
///         >WorkSet.cs
///         >WorkSetreferenceBuilder.cs
///     -Funtions:
///         >MakeReference
/// </summary>
public static class WorkSetReferenceBuilderImplementation
{
    public static Uri MakeReference(string templateName, Guid id, bool template = false, bool vm = false, string? queryParameters = null)
    {
        var sb = new StringBuilder();
        sb.Append("bform://");
        sb.Append(nameof(WorkSet));

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

public class WorkSetReferenceBuilder : IEntityReferenceBuilder
{
    public Uri MakeReference(string templateName, Guid id, bool template = false, bool vm = false, string? queryParameters = null)
    {
        return WorkSetReferenceBuilderImplementation.MakeReference(templateName, id, template, vm, queryParameters);
    }
}
