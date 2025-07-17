namespace BFormDomain.CommonCode.Platform.WorkItems;

public class Section
{
    public int TemplateId { get; set; }
    public List<Uri> Entities { get; set; } = new();
}
