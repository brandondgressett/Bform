namespace BFormDomain.CommonCode.Platform.Forms;

public class CreateFormInstancesCommand
{
    public string TemplateName { get; set; } = "";
    public string? InitialPropertiesName { get; set; }
    public FormInstanceHome Home { get; set; }
    public List<string>? InitialTags { get; set; }

    public List<string> WorkSetTags { get; set; } = null!;
    public List<string> WorkItemTags { get; set; } = null!;

}
