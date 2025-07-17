namespace BFormDomain.CommonCode.Platform.WorkSets;

public class CreateWorkSetInstanceCommand
{
    public string TemplateName { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public List<string> Tags { get; set; } = new();


}
