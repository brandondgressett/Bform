using BFormDomain.CommonCode.Authorization;
using BFormDomain.Validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.WorkSets;

public class WorkSetSummaryViewModel
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

   
    public WorkSetInteractivityState InteractivityState { get; set; }

   
    public WorkSetManagement Management { get; set; }


    public static async Task<WorkSetSummaryViewModel> Create(
        WorkSet workSet, WorkSetTemplate template,
        UserInformationCache users)
    {
        string ownerName = string.Empty;
        if (workSet.ProjectOwner.HasValue)
        {
            var ownerInfo = (await users.Fetch(workSet.ProjectOwner.Value))!;
            if(ownerInfo is not null)
                ownerName = ownerInfo.UserName;
        }

        return new WorkSetSummaryViewModel
        {
            Title = workSet.Title,
            Description = workSet.Description,
            OwnerUserName = ownerName,
            Tags = workSet.Tags,
            TemplateName = workSet.Template,
            IsVisibleToUsers = template.IsVisibleToUsers,
            TemplateTags = template.Tags,
            MenuItem = template.MenuItem,
            NotificationGroupTags = template.NotificationGroupTags,
            InteractivityState = workSet.InteractivityState,
            Management = template.Management,
            Home = template.Home

        };
    }

}
