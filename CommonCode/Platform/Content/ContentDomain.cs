using BFormDomain.HelperClasses;
using BFormDomain.Validation;
using Newtonsoft.Json.Schema;

namespace BFormDomain.CommonCode.Platform.Content;


/// <summary>
/// Represents a type of content to be loaded, 
/// eg, project template, form template, rule
/// </summary>
public class ContentDomain
{
    /// <summary>
    /// Type name
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Json schema defining structure for content elements of this domain.
    /// To create a schema for content entities, create the json schema in
    /// an assembly named "BForm-Schema-{Type Name}.json" and 
    /// Make the build action "embedded resource".
    /// </summary>
    public JSchema? Schema { get; set; }

    /// <summary>
    /// Type to load content elements into.
    /// </summary>
    public Type? ContentType { get; set; }

    private string ContentTypeName 
    { 
        get
        {
            ContentType.Requires().IsNotNull();
            var retval = $".{ContentType!.GetFriendlyTypeName()}";
            return retval;
        } 
    }

    /// <summary>
    /// Defines the file extension for this content type.
    /// </summary>
    public string Extension 
    {   
        get
        {
            var name = ContentTypeName.Replace("Template", "");
            var retval = name + ".json";
            
            return retval;
        } 
    }


    public string InstanceExtension
    {
        get
        {
            var name = ContentTypeName.Replace("Template", "");
            var retval = name + ".i.json";

            return retval;
        }
    }

    public int InstanceGroupDescOrder { get; set; }
}
