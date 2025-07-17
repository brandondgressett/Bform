namespace BFormDomain.CommonCode.Platform.WorkSets;

public class CreatableWorkItem
{
    public string TemplateName { get; set; } = null!;
    public List<string> Tags { get; set; } = new();
    public bool UserCreatable { get; set; }
    public bool CreateOnInitialization { get; set; }

    public string Title { get; set; } = null!;
    
}
