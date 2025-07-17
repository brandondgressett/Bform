using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BFormDomain.CommonCode.Platform.WorkItems;

public class StatusTemplate
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;

    [JsonConverter(typeof(StringEnumConverter))]
    public StatusType StatusType { get; set; }

    public bool IsListedActive { get; set; } = true;
    public bool IsListedInactive { get; set; }

    public bool IsDefault { get; set; }

}
