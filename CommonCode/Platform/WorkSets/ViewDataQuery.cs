using BFormDomain.CommonCode.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BFormDomain.CommonCode.Platform.WorkSets;


public enum ViewDataOrdering
{
    Score,
    Order,
    Recency,
    Grouping
}



public class ViewDataQuery
{
    public double? ScoreThreshold { get; set; }

    public int? Limit { get; set; }
    
    public string? Grouping { get; set; }

    public TimeFrame? Recency { get; set; }

    public string? EntityType { get; set; }
    
    public string? TemplateName { get; set; }

    public List<string> FindAllTags { get; set; } = new();

    public List<string> FindAnyTags { get; set; } = new();


    [JsonConverter(typeof(StringEnumConverter))]
    public ViewDataOrdering Ordering { get; set; }

   
    public List<string> AddMetaTags { get; set; } = new();

    
}
