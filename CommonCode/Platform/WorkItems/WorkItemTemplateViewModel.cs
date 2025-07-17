namespace BFormDomain.CommonCode.Platform.WorkItems;

public class WorkItemTemplateViewModel
{
   
    public string Name { get; set; } = null!;

 
    public string Title { get; set; } = "";

    public int DescendingOrder { get; set; }

    public bool IsVisibleToUsers { get; set; }


    public List<string> Tags { get; set; } = new();
   

    public List<StatusTemplate> StatusTemplates { get; set; } = new();
    public List<TriageTemplate> TriageTemplates { get; set; } = new();
    public List<PriorityTemplate> PriorityTemplates { get; set; } = new();

    public List<SectionTemplate> SectionTemplates { get; set; } = new();

    

    public bool AllowComments { get; set; } = true;
    public bool AllowFileAttachments { get; set; } = true;
    public bool AllowDeletion { get; set; } = true;
    public bool AllowLinks { get; set; } = true;
    public bool AllowBookmarks { get; set; } = true;

    public bool TrackStatus { get; set; } = true;
    public bool TrackAssignee { get; set; } = true;

    public bool TrackPriority { get; set; } = true;
    public bool TrackEventHistory { get; set; } = true;
    public bool TrackUnresolvedLength { get; set; } = true;

    public static WorkItemTemplateViewModel Create(WorkItemTemplate template)
    {
        return new WorkItemTemplateViewModel
        {
            Name = template.Name,
            Title = template.Title,
            DescendingOrder = template.DescendingOrder,
            IsVisibleToUsers = template.IsVisibleToUsers,
            Tags = template.Tags,
            StatusTemplates = template.StatusTemplates,
            TriageTemplates = template.TriageTemplates,
            PriorityTemplates = template.PriorityTemplates,
            SectionTemplates = template.SectionTemplates,
            AllowComments = template.AllowComments,
            AllowFileAttachments = template.AllowFileAttachments,
            AllowDeletion = template.AllowDeletion,
            AllowLinks = template.AllowLinks,
            AllowBookmarks = template.AllowBookmarks,
            TrackStatus = template.TrackStatus,
            TrackAssignee = template.TrackAssignee,
            TrackPriority = template.TrackPriority,
            TrackEventHistory = template.TrackEventHistory,
            TrackUnresolvedLength = template.TrackUnresolvedLength
        };

    }
}
