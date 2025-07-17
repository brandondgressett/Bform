using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Platform.Scheduler;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.Forms;


public class FormTemplate: IContentType
{
    [Required]
    public string? DomainName { get; set;  } = nameof(FormTemplate);

    [Required]
    public string Name { get; set; } = null!;

    public string? Comments { get; set; } = null!;

    public string Title { get; set; } = "";


    public int DescendingOrder { get; set; }

    public string? SubmitTitle { get; set; } = null!;

    [Required]
    public bool ContentSchemaNeedsReplacements { get; set; }

    [Required]
    public bool IsVisibleToUsers { get; set; }

    public bool EventsOnly { get; set; }
    public bool RevertToDefaultsOnSubmit { get; set; }

    [Required]
    public List<string> Tags { get; set; } = new();
        
    public Dictionary<string, string> SatelliteData { get; private set; } = new();

    [JsonIgnore]
    public SatelliteJson ContentSchema { get; set; } = null!;

    [JsonIgnore]
    public SatelliteJson UISchema { get; set; } = null!;

    // https://stackoverflow.com/questions/56725383/create-dynamic-yup-validation-schema-from-json
    [JsonIgnore]
    public SatelliteJson YupSchema { get; set; } = null!;

    public List<ScheduledEventTemplate> Schedules { get; set; } = new();
    
    public string? IconClass { get; set; }

    /// <summary>
    /// optional, replace submit button with these, which can either
    /// submit or use FormLogic.InvokeCustomAction
    /// </summary>
    public List<ActionButton> ActionButtons { get; set; } = new();


    public FormTemplate()
    {
        ContentSchema = new SatelliteJson(this, nameof(ContentSchema));
        UISchema = new SatelliteJson(this, nameof(UISchema));
        YupSchema = new SatelliteJson(this, nameof(YupSchema));
    }
}
