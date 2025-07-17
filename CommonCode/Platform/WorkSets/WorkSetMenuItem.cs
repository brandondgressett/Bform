namespace BFormDomain.CommonCode.Platform.WorkSets;

public class WorkSetMenuItem
{
    public int DescendingOrder { get; set; }
    public string Title { get; set; } = null!;
    public string IconClass { get; set; } = null!;
    public bool IsDefaultMenuItem { get; set; }
    public bool IsVisible { get; set; }
}
