namespace BFormDomain.CommonCode.Platform.WorkItems;


public class WorkItemEventHistoryViewModel: WorkItemEventHistory
{
    public string? UserAssigneeName { get; set; }
    public string? TriageAssigneeTitle { get; set; }
    public string? StatusTitle { get; set; }
    public string? PriorityTitle { get; set; }

    public string? ModifierUserName { get; set; }
}
