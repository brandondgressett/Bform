namespace BFormDomain.CommonCode.Platform.WorkItems;

public class WorkItemEventHistory
{
    public DateTime EventTime { get; set; }
    public Guid? UserAssignee { get; set; }
    public int? TriageAssignee { get; set; }
    public int Status { get; set; }
    public int Priority { get; set; }
     
    public Guid? Modifier { get; set; }
    
}
