namespace BFormDomain.CommonCode.Platform.WorkSets;

public class InitialViewData
{
    public List<string> WorkItemAnyTags { get; set; } = new();
    public List<string> AttachedEntityAnyTags { get; set; } = new();

    public int LimitMatch { get; set; }

    public int DescendingOrder { get; set; }
    public string? Grouping { get; set; }



}
