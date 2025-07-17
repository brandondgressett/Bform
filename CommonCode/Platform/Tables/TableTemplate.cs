using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Platform.Scheduler;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.Tables;

public class TableTemplate : IContentType
{
    [Required]
    public string Name { get; set; } = null!;

    

    public int DescendingOrder { get; set; }

    [Required]
    public string? DomainName { get; set; } = nameof(TableTemplate);

    public Dictionary<string, string>? SatelliteData { get; set; } = new();

    [Required]
    public List<string> Tags { get; set; } = new();

    public string? Comment { get; set; }

    public string? Title { get; set; }
    public string? Description { get; set; }

    [Required]
    public string CollectionName { get; set; } = null!;

    public Guid? CollectionId { get; set; }


    public bool IsVisibleToUsers { get; set; } = true;
    public bool IsUserEditAllowed { get; set; }
    public bool IsUserDeleteAllowed { get; set; }
    public bool IsUserAddAllowed { get; set; }

    public bool DisplayMasterDetail { get; set; }
    public string? DetailFormTemplate { get; set; }

    public bool IsDataGroomed { get; set; }

    public int MonthsRetained { get; set; }
    public int DaysRetained { get; set; }
    public int HoursRetained { get; set; }
    public int MinutesRetained { get; set; }

    public string? IconClass { get; set; }

    public List<ColDef> Columns { get; set; } = new();

    public bool IsPerWorkSet {get;set;}
    public bool IsPerWorkItem { get; set; }

    [JsonIgnore]
    public SatelliteJson AgGridColumnDefs { get; set; }


    public List<ScheduledEventTemplate> Schedules { get; set; } = new();




    public TableTemplate()
    {
        AgGridColumnDefs = new SatelliteJson(this, nameof(AgGridColumnDefs));
    }
}
