using BFormDomain.CommonCode.Authorization;
using BFormDomain.CommonCode.Platform.Comments;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.Reports;


public class ReportInstanceSummaryViewModel
{
    public Guid Id { get; set; }

    
    public string Description { get; set; } = null!;
 
    public string? IconClass { get; set; }

    public string Title { get; set; } = null!;
    
  

    public Guid? WorkSet { get; set; }
    public Guid? WorkItem { get; set; }
    public string Creator { get; set; } = null!;
    public DateTime Created { get; set; }

    public DateTime? GroomDate { get; set; }

    public List<string> Tags { get; set; } = new();

    public static async Task<ReportInstanceSummaryViewModel> CreateSummary(
        ReportInstance report,
        ReportTemplate template, 
        string tzId, 
        UserInformationCache userInfo)
    {
        var localTz = TimeZoneInfo.FromSerializedString(tzId);
        ApplicationUserViewModel? user = null!;
        if (report.Creator is not null)
            user = await userInfo.Fetch(report.Creator.Value);
        string userName = "unknown";
        if(user is not null)
            userName = user.UserName;
        DateTime? groomDate = null!;
        if (report.GroomDate is not null)
            groomDate = TimeZoneInfo.ConvertTimeFromUtc(report.GroomDate.Value, localTz);

        var summary = new ReportInstanceSummaryViewModel
        {
            Id = report.Id,
            Description = template.Description,
            IconClass = template.IconClass,
            Title = report.Title,
            Created = TimeZoneInfo.ConvertTimeFromUtc(report.CreatedDate, localTz),
            WorkSet = report.HostWorkSet,
            WorkItem = report.HostWorkItem,
            Creator = userName,
            GroomDate = groomDate,
            Tags = report.Tags.ToList()
        };

        return summary;
    }
}

public class ReportInstanceViewModel: ReportInstanceSummaryViewModel
{
    
    public string Html { get; set; } = null!;
    
    public List<CommentViewModel> Comments { get; set; } = new();


    public static async Task<ReportInstanceViewModel> CreateView(
        ReportInstance report,
        ReportTemplate template,
        string tzId,
        UserInformationCache userInfo,
        CommentsLogic commentsLogic
        )
    {
        var localTz = TimeZoneInfo.FromSerializedString(tzId);
        ApplicationUserViewModel? user = null!;
        if (report.Creator is not null)
            user = await userInfo.Fetch(report.Creator.Value);
        string userName = "unknown";
        if (user is not null)
            userName = user.UserName;
        DateTime? groomDate = null!;
        if (report.GroomDate is not null)
            groomDate = TimeZoneInfo.ConvertTimeFromUtc(report.GroomDate.Value, localTz);


        var comments = await commentsLogic.GetEntityComments(report.Id, tzId, 0);

        var summary = new ReportInstanceViewModel
        {
            Id = report.Id,
            Description = template.Description,
            IconClass = template.IconClass,
            Title = report.Title,
            Created = TimeZoneInfo.ConvertTimeFromUtc(report.CreatedDate, localTz),
            WorkSet = report.HostWorkSet,
            WorkItem = report.HostWorkItem,
            Creator = userName,
            GroomDate = groomDate,
            Tags = report.Tags.ToList(),
            Html = report.Html,
            Comments = comments.ToList()
        };

        return summary;
    }

}
