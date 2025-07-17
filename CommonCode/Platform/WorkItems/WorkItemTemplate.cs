using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.WorkItems;



public class WorkItemTemplate : IContentType
{
    #region IContentType
    [Required]
    public string? DomainName { get; set; } = null!;

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


    public List<StatusTemplate> StatusTemplates { get; set; } = new();
    public List<TriageTemplate> TriageTemplates { get; set; } = new();
    public List<PriorityTemplate> PriorityTemplates { get; set; } = new();

    public List<SectionTemplate> SectionTemplates { get; set; } = new();

    public bool IsGroomable {get;set;}
    public TimeFrame? GroomPeriod { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public WorkItemGroomBehavior GroomBehavior { get; set; }

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

  
    
    
}

