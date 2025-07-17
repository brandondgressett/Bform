namespace BFormDomain.CommonCode.Platform.Forms;

/// <summary>
/// Represents a custom button for a form, tied to an action that produces events.
/// To be rendered client-side using the "children" property of the form component:
/// https://react-jsonschema-form.readthedocs.io/en/latest/api-reference/form-props/
/// </summary>
public class ActionButton
{
    public string? Text { get; set; }
    public bool IsSubmit { get; set; }
    public string? IconClass { get; set; }

    public int Id { get; set; }
    public string EventTopic { get; set; } = null!;

  
}
