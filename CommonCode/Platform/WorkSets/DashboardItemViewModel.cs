using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.WorkSets;

public class DashboardItemViewModel
{
    public int DescendingOrder { get; set; }



    public string? Grouping { get; set; }


    public string EntityRef { get; set; } = null!;


    public string EntityType { get; set; } = null!;


    public string? TemplateName { get; set; } = null!;

    public JObject Entity { get; set; } = null!;


    public List<string> Tags { get; set; } = new();

    public List<string> MetaTags { get; set; } = new();

}
