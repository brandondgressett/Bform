using BFormDomain.CommonCode.Authorization;
using BFormDomain.Validation;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.WorkItems;

public class WorkItemSummaryViewModel
{
    public string TemplateName { get; set; } = null!;
    public Guid? HostWorkSet { get; set; }

    public List<string> Tags { get; set; } = new List<string>();

    public bool IsListed { get; set; }

    public bool IsVisible { get; set; }

    public Guid? UserAssignee { get; set; }
    public string? UserAssigneeName { get; set; }
    public string? TriageAssignee { get; set; }

    public string Title { get; set; } = null!;

    public string? StatusTitle { get; set; }

    public string? PriorityTitle { get; set; }

    public static async Task<WorkItemSummaryViewModel> Create(
        WorkItemTemplate template, WorkItem item, 
        UserInformationCache uic)
    {
        
        template.Name.Requires().IsEqualTo(item.Template);

 

        string? triageAssignee = null!;
        if (item.TriageAssignee is not null)
        {
            var triage = template.TriageTemplates.First(it => it.TriageId == item.TriageAssignee.Value);
            triageAssignee = triage.Title;
        }
        
        var status = template.StatusTemplates.First(it => it.Id == item.Status);
        string statusTitle = status.Title;

        var priority = template.PriorityTemplates.First(it => it.Id == item.Priority);
        string priorityTitle = priority.Title;

        var retval = new WorkItemSummaryViewModel
        {
            TemplateName = template.Name,
            HostWorkSet = item.HostWorkSet,
            Tags = item.Tags,
            IsListed = item.IsListed,
            IsVisible = item.IsVisible,
            UserAssignee = item.UserAssignee,
            TriageAssignee = triageAssignee,
            Title = template.Title,
            StatusTitle = statusTitle,
            PriorityTitle = priorityTitle
        };

        if(item.UserAssignee is not null)
        {
            var user = (await uic.Fetch(item.UserAssignee.Value))!;
            user.Guarantees().IsNotNull();
            retval.UserAssigneeName = user.UserName;
        }

        return retval;
    }
}
