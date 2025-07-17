using BFormDomain.CommonCode.Authorization;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.Validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.WorkSets;

public class WorkSetViewModel
{
    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string OwnerUserName { get; set; } = null!;

    public List<string> Tags { get; set; } = new List<string>();

    public string TemplateName { get; set; } = null!;
    
    public bool IsVisibleToUsers { get; set; }
    public List<string> TemplateTags { get; set; } = new();

    [JsonConverter(typeof(StringEnumConverter))]
    public WorkSetHome Home { get; set; }
    public WorkSetMenuItem? MenuItem { get; set; }

    public List<string> NotificationGroupTags { get; set; } = new();

    [JsonRequired]
    public WorkSetInteractivityState InteractivityState { get; set; }

    [JsonRequired]
    public WorkSetManagement Management { get; set; }

    [JsonProperty(ItemConverterType = typeof(ViewRowDefConverter))]
    public List<ViewRowDef> View { get; set; } = new();

    public bool IsEveryoneAMember { get; set; }

    public List<CreatableWorkItem> WorkItemCreationTemplates { get; set; } = new();

    public List<DashboardItemViewModel> DashboardData { get; set; } = new();

    public static async Task<WorkSetViewModel> Create(
        WorkSet ws, 
        WorkSetTemplate template,
        List<DashboardCandidate> dashboardWinners,
        EntityReferenceLoader loader,
        UserInformationCache users)
    {
        
        string ownerName = string.Empty;
        if (ws.ProjectOwner.HasValue)
        {
            var ownerInfo = (await users.Fetch(ws.ProjectOwner.Value))!;
            if (ownerInfo is not null)
                ownerName = ownerInfo.UserName;
        }

        List<DashboardItemViewModel> dashboardData = new();
        List<(Task<JObject?>,DashboardCandidate)> work = new();
        foreach(var dw in dashboardWinners)
        {
            work.Add((loader.LoadEntityJsonFromReference(dw.EntityRef),dw));
        }
        await Task.WhenAll(work.Select(it=>it.Item1));

        foreach(var res in work)
        {
            var (task, dw) = res;
            var entity = task.Result;

            if(entity is not null)
            {
                dashboardData.Add(new DashboardItemViewModel
                {
                    DescendingOrder = dw.DescendingOrder,
                    Grouping = dw.Grouping,
                    EntityRef = dw.EntityRef,
                    EntityType = dw.EntityType,
                    Entity = entity,
                    Tags = dw.Tags,
                    MetaTags = dw.MetaTags
                });
            }
        }

        

        return new WorkSetViewModel
        {
            Title = ws.Title,
            Description = ws.Description,
            OwnerUserName = ownerName,
            Tags = ws.Tags.ToList(),
            TemplateName = template.Name,
            IsVisibleToUsers = template.IsVisibleToUsers,
            TemplateTags = template.Tags.ToList(),
            MenuItem = template.MenuItem,
            NotificationGroupTags = template.NotificationGroupTags.ToList(),
            InteractivityState = ws.InteractivityState,
            Management = template.Management,
            View = template.View.ToList(),
            IsEveryoneAMember = template.IsEveryoneAMember,
            WorkItemCreationTemplates = template.WorkItemCreationTemplates.ToList(),
            DashboardData = dashboardData,
            Home = template.Home

        };

    }

}
