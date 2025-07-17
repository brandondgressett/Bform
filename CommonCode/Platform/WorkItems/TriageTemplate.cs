namespace BFormDomain.CommonCode.Platform.WorkItems;

public class TriageTemplate
{
    public int TriageId { get; set; }
    public string? UserTag { get; set; }
    public string Title { get; set; } = null!;

    public bool IsInitialAssigneeOnNew { get; set; }
    public int? ForceStatus { get; set; }

}
