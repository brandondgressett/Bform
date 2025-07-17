using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.ManagedFiles;
using System.Text;

namespace BFormDomain.CommonCode.Platform.ManagedFile;

public static class ManagedFileReferenceBuilderImplementation
{
    public static Uri MakeReference(string templateName, Guid id,
        bool template = false, bool vm = false, string? queryParameters = null)
    {
        var sb = new StringBuilder();


        sb.Append("bform://");
        sb.Append(nameof(ManagedFileInstance));

        if (vm)
            sb.Append("/vm/");

        sb.Append(id);
        return new Uri(sb.ToString());

    }
}


public class ManagedFileReferenceBuilder : IEntityReferenceBuilder
{
    public Uri MakeReference(string templateName, Guid id, 
        bool template = false, bool vm = false, string? queryParameters = null)
    {
        return ManagedFileReferenceBuilderImplementation.MakeReference(templateName, id, template, vm, queryParameters);
    }
}
