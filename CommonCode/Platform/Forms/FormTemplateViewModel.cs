using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Utility;
using BFormDomain.Validation;
using Newtonsoft.Json.Schema;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.Forms;

public class FormTemplateViewModel
{
    public string Name { get; set; } = null!;
    public string Title { get; set; } = "";
    public int DescendingOrder { get; set; }
    public string? SubmitTitle { get; set; }
    public bool IsVisibleToUsers { get; set; }
    public bool EventsOnly { get; set; }
    public bool RevertToDefaultsOnSubmit { get; set; }
    public string ContentSchema { get; set; } = "";
    public string? UISchema { get; set; }
    public string? YupSchema { get; set; }

    public string? IconClass { get; set; }

    public string? DefaultProperties { get; set; }

    public static FormTemplateViewModel Create(
        FormTemplate template,
        IApplicationTerms terms)
    {
        

        //var contentSchemaJson = template.ContentSchema.Json?.ToString()!;
        var contentSchemaJson = template.SatelliteData["contentschema"].ToString();
        contentSchemaJson.Guarantees().IsNotNull();

        if (template.ContentSchemaNeedsReplacements)
        {
            contentSchemaJson = terms.ReplaceTerms(contentSchemaJson);
        }

        var uiSchemaJson = template.UISchema?.Json?.ToString();
        var yupSchemaJson = template.YupSchema?.Json?.ToString();

        var schema = JSchema.Parse(contentSchemaJson!);
        var initialPropertiesJson = JsonFromSchema.Generate(schema)?.ToString();

        var retval = new FormTemplateViewModel
        {
            Name = template.Name,
            ContentSchema = contentSchemaJson ?? "",
            DescendingOrder = template.DescendingOrder,
            IsVisibleToUsers = template.IsVisibleToUsers,
            EventsOnly = template.EventsOnly,
            RevertToDefaultsOnSubmit = template.RevertToDefaultsOnSubmit,
            SubmitTitle = template.SubmitTitle is null ? string.Empty : terms.ReplaceTerms(template!.SubmitTitle),
            Title = terms.ReplaceTerms(template.Title),
            UISchema = uiSchemaJson,
            YupSchema = yupSchemaJson,
            IconClass = template.IconClass,
            DefaultProperties = initialPropertiesJson
        };

        return retval;
    }
}
