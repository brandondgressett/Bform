using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Platform.Scheduler;
using BFormDomain.CommonCode.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.WorkSets;

public class WorkSetTemplate : IContentType
{

    public WorkSetTemplate()
    {
        // default value for dashboard schedule
        DashboardSchedule = new ScheduledEventTemplate
        {
            Name = $"{this.Name}_dashboard_schedule",
            DescendingOrder = 1000,
            EventTopic = "workset_build_dashboard",
            Schedule = "rf:0.00:10"
        };

        OldToGroom = new TimeFrame { TimeFrameDays = 1 };
    }



    #region IContentType
    [Required]
    public string? DomainName { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public string? Comments { get; set; } = null!;

    public string Title { get; set; } = "";

    [Required]
    public int DescendingOrder { get; set; }

    [Required]
    public bool IsVisibleToUsers { get; set; }
    
    public Dictionary<string, string> SatelliteData { get; private set; } = new();

    public List<string> Tags { get; set; } = new();
    #endregion

    [JsonConverter(typeof(StringEnumConverter))]
    public WorkSetHome Home { get; set; }
    public WorkSetMenuItem? MenuItem { get; set; }


    public List<string> NotificationGroupTags { get; set; } = new();

    [JsonRequired]
    public WorkSetInteractivityState StartingInteractivityState { get; set; }

    [JsonRequired]
    public WorkSetManagement Management { get; set; }

    public bool IsUserCreatable { get; set; }
    

    public double DashboardBuildDeferralSeconds { get; set; } = 90.0;

    public ScheduledEventTemplate DashboardSchedule { get; set; }

    [JsonProperty(ItemConverterType = typeof(ViewRowDefConverter))]
    public List<ViewRowDef> View { get; set; } = new();

    public List<InitialViewData> ViewDataInitialization { get; set; } = new();
    
    public TimeFrame OldToGroom { get; set; }

    public bool IsEveryoneAMember { get; set; }

    public List<CreatableWorkItem> WorkItemCreationTemplates { get; set; } = new();
    

}


